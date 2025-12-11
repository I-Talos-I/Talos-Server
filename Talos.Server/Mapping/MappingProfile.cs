using AutoMapper;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // TemplateCreateDto - Template
        CreateMap<TemplateCreateDto, Template>()
            .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.Template_Name))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.Is_Public))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => src.License_Type))
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.CreateAt, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TemplateDependencies, opt => opt.Ignore());

        // Template - TemplateDto
        CreateMap<Template, TemplateDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.User_Id, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Template_Name, opt => opt.MapFrom(src => src.TemplateName))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Is_Public, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.License_Type, opt => opt.MapFrom(src => src.LicenseType))
            .ForMember(dest => dest.Create_At, opt => opt.MapFrom(src => src.CreateAt));
    }
}