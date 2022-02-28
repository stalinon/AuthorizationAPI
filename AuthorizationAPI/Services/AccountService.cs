using AuthorizationAPI.Authorization;
using AuthorizationAPI.DataAccess;
using AuthorizationAPI.Entities;
using AuthorizationAPI.Helpers;
using AuthorizationAPI.Helpers.Exceptions;
using AuthorizationAPI.Models.Dto;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthorizationAPI.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repo;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly AuthSettings _appSettings;
        private readonly IEmailService _emailService;

        public AccountService(
            IAccountRepository repo,
            IJwtUtils jwtUtils,
            IMapper mapper,
            IOptions<AuthSettings> appSettings,
            IEmailService emailService)
        {
            _repo = repo;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _emailService = emailService;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var account = _repo.FirstOrDefault(x => x.Email == model.Email);

            if (account == null || !account.IsVerified || !BCrypt.Net.BCrypt.Verify(model.Password, account.PasswordHash))
                throw new AuthException("Email or password is incorrect");

            var jwtToken = _jwtUtils.GenerateJwtToken(account);
            var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            account.RefreshTokens.Add(refreshToken);

            RemoveOldRefreshTokens(account);

            _repo.Update(account);
            _repo.Save(); 

            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            return response;
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var account = GetAccountByRefreshToken(token);
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

            if (refreshToken.IsRevoked)
            {
                // revoke all descendant tokens in case this token has been compromised
                RevokeDescendantRefreshTokens(refreshToken, account, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
                _repo.Update(account);
                _repo.Save();
            }

            if (!refreshToken.IsActive)
                throw new AuthException("Invalid token");

            // replace old refresh token with a new one (rotate token)
            var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
            account.RefreshTokens.Add(newRefreshToken);


            // remove old refresh tokens from account
            RemoveOldRefreshTokens(account);

            // save changes to db
            _repo.Update(account);
            _repo.Save();

            // generate new jwt
            var jwtToken = _jwtUtils.GenerateJwtToken(account);

            // return data in authenticate response object
            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var account = GetAccountByRefreshToken(token);
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
                throw new AuthException("Invalid token");

            // revoke token and save
            RevokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
            _repo.Update(account);
            _repo.Save();
        }

        public void Register(RegisterRequest model, string origin)
        {
            var account = _repo.FirstOrDefault(x => x.Email == model.Email);
            // validate
            if (account != null)
            {
                // send already registered error in email to prevent account enumeration
                //SendAlreadyRegisteredEmail(model.Email, origin);
                return;
            }

            // map model to new account object
            account = _mapper.Map<Account>(model);

            // first registered account is an admin
            var isFirstAccount = _repo.GetAll().Count() == 0;
            account.Role = isFirstAccount ? Role.Admin : Role.User;
            account.Created = DateTime.UtcNow;
            account.VerificationToken = GenerateVerificationToken();

            // hash password
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // save account
            _repo.Add(account);
            _repo.Save();

            // send email
            //SendVerificationEmail(account, origin);
        }

        public void VerifyEmail(string token)
        {
            var account = _repo.FirstOrDefault(x => x.VerificationToken == token);

            if (account == null)
                throw new AuthException("Verification failed");

            account.Verified = DateTime.UtcNow;
            account.VerificationToken = null;

            _repo.Update(account);
            _repo.Save();
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = _repo.FirstOrDefault(x => x.Email == model.Email);

            // always return ok response to prevent email enumeration
            if (account == null) return;

            // create reset token that expires after 1 day
            account.ResetToken = GenerateResetToken();
            account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

            _repo.Update(account);
            _repo.Save();

            // send email
            //SendPasswordResetEmail(account, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            GetAccountByResetToken(model.Token);
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            var account = GetAccountByResetToken(model.Token);

            // update password and remove reset token
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            account.PasswordReset = DateTime.UtcNow;
            account.ResetToken = null;
            account.ResetTokenExpires = null;

            _repo.Update(account);
            _repo.Save();
        }

        public IEnumerable<AccountResponse> GetAll()
        {
            var accounts = _repo.GetAll();
            return _mapper.Map<IList<AccountResponse>>(accounts);
        }

        public AccountResponse GetById(int id)
        {
            var account = GetAccount(id);
            return _mapper.Map<AccountResponse>(account);
        }

        public AccountResponse Create(CreateRequest model)
        {
            var account = _repo.FirstOrDefault(x => x.Email == model.Email);
            // validate
            if (account != null)
                throw new AuthException($"Email '{model.Email}' is already registered");

            // map model to new account object
            account = _mapper.Map<Account>(model);
            account.Created = DateTime.UtcNow;
            account.Verified = DateTime.UtcNow;

            // hash password
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // save account
            _repo.Add(account);
            _repo.Save();

            return _mapper.Map<AccountResponse>(account);
        }

        public AccountResponse Update(int id, UpdateRequest model)
        {
            var account = GetAccount(id);
            var user = _repo.FirstOrDefault(x => x.Email == model.Email);
            // validate
            if (account.Email != model.Email && user != null)
                throw new AuthException($"Email '{model.Email}' is already registered");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // copy model to account and save
            _mapper.Map(model, account);
            account.Updated = DateTime.UtcNow;
            _repo.Update(account);
            _repo.Save();

            return _mapper.Map<AccountResponse>(account);
        }

        public void Delete(int id)
        {
            var account = GetAccount(id);
            _repo.Remove(account);
            _repo.Save();
        }

        // helper methods

        private Account GetAccount(int id)
        {
            var account = _repo.Find(id);
            if (account == null) throw new KeyNotFoundException("Account not found");
            return account;
        }

        private Account GetAccountByRefreshToken(string token)
        {
            var account = _repo.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            if (account == null) throw new AuthException("Invalid token");
            return account;
        }

        private Account GetAccountByResetToken(string token)
        {
            var account = _repo.FirstOrDefault(x =>
                x.ResetToken == token && x.ResetTokenExpires > DateTime.UtcNow);
            if (account == null) throw new AuthException("Invalid token");
            return account;
        }

        private string GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateResetToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            // ensure token is unique by checking against db
            var tokenIsUnique = _repo.FirstOrDefault(x => x.ResetToken == token) == null;
            if (!tokenIsUnique)
                return GenerateResetToken();

            return token;
        }

        private string GenerateVerificationToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            // ensure token is unique by checking against db
            var tokenIsUnique = _repo.FirstOrDefault(x => x.ResetToken == token) == null;
            if (!tokenIsUnique)
                return GenerateVerificationToken();

            return token;
        }

        private RefreshToken RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            RevokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
            return newRefreshToken;
        }

        private void RemoveOldRefreshTokens(Account account)
        {
            account.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }

        private void RevokeDescendantRefreshTokens(RefreshToken refreshToken, Account account, string ipAddress, string reason)
        {
            // recursively traverse the refresh token chain and ensure all descendants are revoked
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var childToken = account.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
                if (childToken.IsActive)
                    RevokeRefreshToken(childToken, ipAddress, reason);
                else
                    RevokeDescendantRefreshTokens(childToken, account, ipAddress, reason);
            }
        }

        private void RevokeRefreshToken(RefreshToken token, string ipAddress, string reason = null, string replacedByToken = null)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            token.ReplacedByToken = replacedByToken;
        }

        private void SendVerificationEmail(Account account, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                // origin exists if request sent from browser single page app (e.g. Angular or React)
                // so send link to verify via single page app
                var verifyUrl = $"{origin}/account/verify-email?token={account.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                            <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                // origin missing if request sent directly to api (e.g. from Postman)
                // so send instructions to verify directly with api
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                            <p><code>{account.VerificationToken}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Verify Email",
                html: $@"<h4>Verify Email</h4>
                        <p>Thanks for registering!</p>
                        {message}"
            );
        }

        private void SendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
                message = $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
            else
                message = "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

            _emailService.Send(
                to: email,
                subject: "Sign-up Verification API - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                        <p>Your email <strong>{email}</strong> is already registered.</p>
                        {message}"
            );
        }

        private void SendPasswordResetEmail(Account account, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/account/reset-password?token={account.ResetToken}";
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                            <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                            <p><code>{account.ResetToken}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                        {message}"
            );
        }
    }
}
