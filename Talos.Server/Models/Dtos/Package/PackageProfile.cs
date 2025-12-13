using AutoMapper;
using Talos.Server.Models.Dtos.Package;

public class PackageProfile : Profile
{
    public PackageProfile()
    {
        CreateMap<PackageCreateDto, Package>();
    }
}