using AutoMapper;
using ElevatorSimulator.DTOs;
using ElevatorSimulator.Models;

namespace ElevatorSimulator.Mappers
{

    public class Mapper : ElevatorSimulator.Mappers.IMapper
    {
        private readonly AutoMapper.IMapper _mapper;

        public Mapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Elevator, ElevatorDto>()
                    .ForMember(dest => dest.ElevatorStatus, opt => opt.MapFrom(src => new ElevatorStatusDto { Status = src.ElevatorStatus.ToString() }))
                    .ForMember(dest => dest.ElevatorDirection, opt => opt.MapFrom(src => new ElevatorDirectionDto { Direction = src.ElevatorDirection.ToString() }));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
                cfg.CreateMap<ElevatorDto, Elevator>()
                    .ForMember(dest => dest.ElevatorStatus, opt => opt.MapFrom(src => Enum.Parse<Elevator.Status>(src.ElevatorStatus.Status)))
                    .ForMember(dest => dest.ElevatorDirection, opt => opt.MapFrom(src => Enum.Parse<Elevator.Direction>(src.ElevatorDirection.Direction)));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                cfg.CreateMap<FloorDto, Floor>()
                    .ForMember(dest => dest.WaitingPassengers, opt => opt.MapFrom(src => src.WaitingPassengers));
                cfg.CreateMap<Floor, FloorDto>()
                    .ForMember(dest => dest.WaitingPassengers, opt => opt.MapFrom(src => src.WaitingPassengers));

                cfg.CreateMap<Passenger, PassengerDto>();

                cfg.CreateMap<PassengerDto, Passenger>();
            });

            _mapper = configuration.CreateMapper();

        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return _mapper.Map<TSource, TDestination>(source);
        }

        public List<TDestination> MapList<TSource, TDestination>(List<TSource> sourceList)
        {
            return _mapper.Map<List<TSource>, List<TDestination>>(sourceList);
        }
    }
}
