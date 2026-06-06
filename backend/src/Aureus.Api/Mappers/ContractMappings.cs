using AutoMapper;
using Aureus.Api.Contracts.Categories;
using Aureus.Domain.Categories;

namespace Aureus.Api.Mappers;

public sealed class ContractMappings : Profile
{
    public ContractMappings()
    {
        CreateMap<Category, CategoryResponse>();
    }
}
