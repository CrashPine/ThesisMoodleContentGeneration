using AutoMapper;
using Backend.BLL.DTOs;
using Backend.DAL.Entities; 

namespace Backend.BLL.Mappings;

public class QuizProfile : Profile
{
    public QuizProfile()
    {
        CreateMap<Test, TestResultDto>();
    }
}