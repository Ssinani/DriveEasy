using AutoMapper;
using VehicleRental.API.DTOs;
using VehicleRental.API.Models;

namespace VehicleRental.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Vehicle mappings
            CreateMap<VehicleCreateDto, Vehicle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.LicensePlate.ToUpper()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Reservations, opt => opt.Ignore());

            CreateMap<VehicleUpdateDto, Vehicle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.LicensePlate.ToUpper()))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Reservations, opt => opt.Ignore());

            CreateMap<Vehicle, VehicleReadDto>();

            // Reservation mappings
            CreateMap<Reservation, ReservationReadDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.VehicleName, opt => opt.MapFrom(src => src.Vehicle != null ? $"{src.Vehicle.Make} {src.Vehicle.Model} ({src.Vehicle.Year})" : string.Empty))
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.LicensePlate : string.Empty))
                .ForMember(dest => dest.DailyRate, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.DailyRate : 0))
                .ForMember(dest => dest.RentalDays, opt => opt.MapFrom(src => src.RentalDays));

            // User mappings
            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));
        }
    }
}
