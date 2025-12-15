using AutoMapper;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;

namespace Talos.Server.Tests.Helpers;

public class AutoMapperTestProfile : Profile
{
    public AutoMapperTestProfile()
    {
        CreateMap<TemplateCreateDto, Template>()
            // Campos que NO vienen del DTO
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.UserId, opt => opt.Ignore())
            .ForMember(d => d.User, opt => opt.Ignore())
            .ForMember(d => d.Dependencies, opt => opt.Ignore())

            // Campos que normalmente se generan en backend
            .ForMember(d => d.Slug, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())

            // Mapeos con nombres distintos
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.IsPublic, opt => opt.MapFrom(s => s.IsPublic))
            .ForMember(d => d.LicenseType, opt => opt.MapFrom(s => s.LicenseType));
    }

    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperTestProfile>();
        });

        config.AssertConfigurationIsValid(); 
        return config.CreateMapper();
    }
}