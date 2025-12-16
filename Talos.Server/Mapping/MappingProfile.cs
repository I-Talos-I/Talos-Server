using AutoMapper;
using Talos.Server.Models;
using Talos.Server.Models.Dtos;
using Talos.Server.Models.Entities;
using OperatingSystem = Talos.Server.Models.Entities.OperatingSystem;

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
            .ForMember(d => d.Commands, o => o.MapFrom(s => MapCommands(s.Commands))); // Manual mapping replaced with custom MapFrom

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
                        .Where(c => c.OS == OperatingSystem.Linux)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList(),
                    Windows = s.Commands
                        .Where(c => c.OS == OperatingSystem.Windows)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList(),
                    MacOS = s.Commands
                        .Where(c => c.OS == OperatingSystem.MacOS)
                        .OrderBy(c => c.Order)
                        .Select(c => c.Command)
                        .ToList()
                }));

    }

    private static List<DependencyCommand> MapCommands(DependencyCommandsDto commandsDto)
    {
        var commands = new List<DependencyCommand>();

        if (commandsDto?.Linux != null)
        {
            for (int i = 0; i < commandsDto.Linux.Count; i++)
            {
                commands.Add(new DependencyCommand
                {
                    OS = OperatingSystem.Linux,
                    Order = i + 1, // Assuming order starts from 1; adjust if needed
                    Command = commandsDto.Linux[i]
                });
            }
        }

        if (commandsDto?.Windows != null)
        {
            for (int i = 0; i < commandsDto.Windows.Count; i++)
            {
                commands.Add(new DependencyCommand
                {
                    OS = OperatingSystem.Windows,
                    Order = i + 1,
                    Command = commandsDto.Windows[i]
                });
            }
        }

        if (commandsDto?.MacOS != null)
        {
            for (int i = 0; i < commandsDto.MacOS.Count; i++)
            {
                commands.Add(new DependencyCommand
                {
                    OS = OperatingSystem.MacOS,
                    Order = i + 1,
                    Command = commandsDto.MacOS[i]
                });
            }
        }

        return commands;
    }
}