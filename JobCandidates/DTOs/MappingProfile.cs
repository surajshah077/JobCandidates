using AutoMapper;
using JobCandidates.Model;

namespace JobCandidates.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<CandidateDto, Candidate>();
            CreateMap<Candidate, CandidateDto>();
        }
    }
}
