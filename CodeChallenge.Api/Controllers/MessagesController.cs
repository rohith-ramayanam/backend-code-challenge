using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageRepository repository, ILogger<MessagesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        _logger.LogInformation("Fetching all messages for organization {OrganizationId}", organizationId);

        var messages = await _repository.GetAllByOrganizationAsync(organizationId);
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        _logger.LogInformation("Fetching message {MessageId} for organization {OrganizationId}", id, organizationId);

        var message = await _repository.GetByIdAsync(organizationId, id);

        if (message == null)
        {
            _logger.LogWarning("Message {MessageId} not found", id);
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<Message>> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Title and Content are required.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdMessage = await _repository.CreateAsync(message);

        _logger.LogInformation("Message created with Id {MessageId}", createdMessage.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, id = createdMessage.Id },
            createdMessage);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        if (request == null) return BadRequest();

        var existing = await _repository.GetByIdAsync(organizationId, id);
        if (existing == null)
        {
            _logger.LogWarning("Update failed: Message {MessageId} not found", id);
            return NotFound();
        }

        // Only update provided fields
        existing.Title = request.Title?.Trim() ?? existing.Title;
        existing.Content = request.Content?.Trim() ?? existing.Content;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);

        _logger.LogInformation("Message {MessageId} updated successfully", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message == null)
        {
            _logger.LogWarning("Delete failed: Message {MessageId} not found", id);
            return NotFound();
        }

        // Soft delete
        message.IsActive = false;
        message.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(message);

        _logger.LogInformation("Message {MessageId} soft-deleted", id);

        return NoContent();
    }
}