using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageLogic _logic;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageLogic logic, ILogger<MessagesController> logger)
    {
        _logic = logic;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
        => Ok(await _logic.GetAllMessagesAsync(organizationId));

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        var message = await _logic.GetMessageAsync(organizationId, id);
        return message is not null ? Ok(message) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        var result = await _logic.CreateMessageAsync(organizationId, request);

        return result switch
        {
            Created<Message> created => CreatedAtAction(nameof(GetById), new { organizationId, id = created.Value.Id }, created.Value),
            ValidationError validation => BadRequest(validation.Errors),
            Conflict conflict => Conflict(new { error = conflict.Message }),
            _ => StatusCode(500)
        };
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        var result = await _logic.UpdateMessageAsync(organizationId, id, request);

        return result switch
        {
            Updated => NoContent(),
            NotFound notFound => NotFound(new { error = notFound.Message }),
            ValidationError validation => BadRequest(validation.Errors),
            Conflict conflict => Conflict(new { error = conflict.Message }),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var result = await _logic.DeleteMessageAsync(organizationId, id);

        return result switch
        {
            Deleted => NoContent(),
            NotFound notFound => NotFound(new { error = notFound.Message }),
            ValidationError validation => BadRequest(validation.Errors),
            _ => StatusCode(500)
        };
    }
}