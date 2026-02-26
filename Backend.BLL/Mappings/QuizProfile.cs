using AutoMapper;
using Backend.BLL.DTOs;
using Backend.DAL.Entities; // Замени на свой namespace сущностей

namespace Backend.BLL.Mappings;

public class QuizProfile : Profile
{
    public QuizProfile()
    {
        // Маппинг из сущности базы данных в DTO для фронтенда
        CreateMap<Test, TestResultDto>();
    }
}