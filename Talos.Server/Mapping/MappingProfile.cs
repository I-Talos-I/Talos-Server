using AutoMapper;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;
using Talos.Server.Models.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // =========================
        // CREATE DTO -> ENTITY
        // =========================

        CreateMap<TemplateCreateDto, Template>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.Slug, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Dependencies, o => o.MapFrom(s => s.Dependencies));

        CreateMap<TemplateDependencyCreateDto, TemplateDependency>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TemplateId, o => o.Ignore())
            .ForMember(d => d.Versions, o => o.MapFrom(s => s.Versions))
            .ForMember(d => d.Commands, o => o.Ignore()); // se arma manualmente

        CreateMap<string, DependencyVersion>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Version, o => o.MapFrom(s => s))
            .ForMember(d => d.TemplateDependencyId, o => o.Ignore());

        // =========================
        // ENTITY -> DTO
        // =========================

        CreateMap<Template, TemplateDto>();

        CreateMap<TemplateDependency, TemplateDependencyDto>()
            .ForMember(d => d.Versions,
                o => o.MapFrom(s =>
                    s.Versions
                     .OrderBy(v => v.Id)
                     .Select(v => v.Version)))
            .ForMember(d => d.Commands,
                o => o.MapFrom(s => new DependencyCommandsDto
                {
                    Linux = s.Commands
                        .Where(c => c.OS == Talos.Server.Models.Entities.OperatingSystem.Linux)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList(),

                    Windows = s.Commands
                        .Where(c => c.OS == Talos.Server.Models.Entities.OperatingSystem.Windows)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList(),

                    MacOS = s.Commands
                        .Where(c => c.OS == Talos.Server.Models.Entities.OperatingSystem.MacOS)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList()
                }));
    }
}
