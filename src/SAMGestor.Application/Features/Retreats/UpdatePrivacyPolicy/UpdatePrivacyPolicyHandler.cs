using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Retreats.UpdatePrivacyPolicy;

public sealed class UpdatePrivacyPolicyHandler 
    : IRequestHandler<UpdatePrivacyPolicyCommand, UpdatePrivacyPolicyResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdatePrivacyPolicyHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<UpdatePrivacyPolicyResponse> Handle(
        UpdatePrivacyPolicyCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var policy = new PrivacyPolicy(
            title: cmd.Title,
            body: cmd.Body,
            version: cmd.Version,
            publishedAt: DateTime.UtcNow
        );
        
        retreat.SetPrivacyPolicy(policy, cmd.ModifiedByUserId);

        await _uow.SaveChangesAsync(ct);

        return new UpdatePrivacyPolicyResponse(
            RetreatId: retreat.Id,
            Title: policy.Title,
            Version: policy.Version,
            PublishedAt: policy.PublishedAt,
            Message: "Política de privacidade atualizada com sucesso."
        );
    }
}