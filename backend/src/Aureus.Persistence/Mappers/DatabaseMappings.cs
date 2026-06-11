using AutoMapper;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Entities;

namespace Aureus.Persistence.Mappers;

public sealed class DatabaseMappings : Profile
{
    public DatabaseMappings()
    {
        CreateMap<User, UserDb>();
        CreateMap<UserDb, User>();
        CreateMap<Workspace, WorkspaceDb>();
        CreateMap<WorkspaceDb, Workspace>();
        CreateMap<WorkspaceMember, WorkspaceMemberDb>()
            .ForMember(
                destination => destination.Role,
                options => options.MapFrom(source => source.Role.ToString()));
        CreateMap<FinancialAccount, FinancialAccountDb>();
        CreateMap<FinancialAccountDb, FinancialAccount>();
        CreateMap<Category, CategoryDb>()
            .ForMember(
                destination => destination.Type,
                options => options.MapFrom(source => source.Type.ToString()));
        CreateMap<CategoryDb, Category>()
            .ForMember(
                destination => destination.Type,
                options => options.MapFrom(source => Enum.Parse<TransactionType>(source.Type)));
        CreateMap<Transaction, TransactionDb>()
            .ForMember(
                destination => destination.Type,
                options => options.MapFrom(source => source.Type.ToString()));
        CreateMap<TransactionDb, Transaction>()
            .ForMember(
                destination => destination.Type,
                options => options.MapFrom(source => Enum.Parse<TransactionType>(source.Type)));
    }
}
