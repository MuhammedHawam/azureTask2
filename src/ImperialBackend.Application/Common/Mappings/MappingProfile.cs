using AutoMapper;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.ValueObjects;

namespace ImperialBackend.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for mapping between domain entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the MappingProfile class
    /// </summary>
    public MappingProfile()
    {
        CreateMap<Outlet, OutletDto>();
        CreateMap<OutletDetail, OutletDetailDto>();
        CreateMap<CreateOutletDto, CreateOutletCommand>();
    }
}