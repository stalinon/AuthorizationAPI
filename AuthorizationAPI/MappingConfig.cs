using AuthorizationAPI.Entities;
using AuthorizationAPI.Models.Dto;
using AutoMapper;

namespace AuthorizationAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<UserDto, User>();
                config.CreateMap<User, UserDto>();
            });

            return mappingConfig;
        }
    }
}
