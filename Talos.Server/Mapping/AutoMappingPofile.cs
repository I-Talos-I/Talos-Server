using AutoMapper;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Mapping
{
    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            // TemplateDependency maps
            CreateMap<TemplateDependencyCreateDto, TemplateDependencyDto>();
            CreateMap<TemplateDependencyCreateDto, TemplateDependencyCreateDto>();
        }
    }
}