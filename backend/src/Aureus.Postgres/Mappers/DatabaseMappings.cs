using AutoMapper;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Postgres.Entities;

namespace Aureus.Postgres.Mappers;

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
    }
}
