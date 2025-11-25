using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using FluentAssertions;
using Moq;

namespace CodeChallenge.Tests;

public class MessageLogicTests
{
    private readonly Mock<IMessageRepository> _repoMock;
    private readonly MessageLogic _logic;
    private readonly Guid _orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public MessageLogicTests()
    {
        _repoMock = new Mock<IMessageRepository>();
        _logic = new MessageLogic(_repoMock.Object);
    }

    [Fact]
    public async Task CreateMessageAsync_ValidRequest_ReturnsCreatedWithMessage()
    {
        var request = new CreateMessageRequest { Title = "Valid Title", Content = "This content is long enough to pass." };
        _repoMock.Setup(r => r.GetByTitleAsync(_orgId, "Valid Title")).ReturnsAsync((Message?)null);

        var result = await _logic.CreateMessageAsync(_orgId, request);

        result.Should().BeOfType<Created<Message>>();
        var created = (Created<Message>)result;
        created.Value.Title.Should().Be("Valid Title");
        created.Value.OrganizationId.Should().Be(_orgId);
        created.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateMessageAsync_DuplicateTitle_ReturnsConflict()
    {
        var request = new CreateMessageRequest 
        { 
            Title = "Exists", 
            Content = "This content is at least 10 characters"   // ← fixed: now >= 10 chars
        };
        _repoMock.Setup(r => r.GetByTitleAsync(_orgId, "Exists"))
                 .ReturnsAsync(new Message { Id = Guid.NewGuid(), Title = "Exists" });

        var result = await _logic.CreateMessageAsync(_orgId, request);

        result.Should().BeOfType<Conflict>();
        ((Conflict)result).Message.Should().Be("A message with this title already exists in the organization.");
    }

    [Fact]
    public async Task CreateMessageAsync_InvalidTitleLength_ReturnsValidationError()
    {
        var request = new CreateMessageRequest 
        { 
            Title = "AB", 
            Content = "This content is long enough to pass validation." 
        };

        var result = await _logic.CreateMessageAsync(_orgId, request);

        result.Should().BeOfType<ValidationError>();
        var errors = (ValidationError)result;
        errors.Errors.Should().ContainKey("title");
    }

    [Fact]
    public async Task UpdateMessageAsync_NonExistentMessage_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_orgId, id)).ReturnsAsync((Message?)null);

        var result = await _logic.UpdateMessageAsync(_orgId, id, new UpdateMessageRequest());

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateMessageAsync_InactiveMessage_ReturnsValidationError()
    {
        var inactiveMessage = new Message { Id = Guid.NewGuid(), IsActive = false };
        _repoMock.Setup(r => r.GetByIdAsync(_orgId, inactiveMessage.Id)).ReturnsAsync(inactiveMessage);

        var result = await _logic.UpdateMessageAsync(_orgId, inactiveMessage.Id, new UpdateMessageRequest());

        result.Should().BeOfType<ValidationError>();
        var error = (ValidationError)result;
        error.Errors.Should().ContainKey("general");
    }

    [Fact]
    public async Task DeleteMessageAsync_NonExistentMessage_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_orgId, id)).ReturnsAsync((Message?)null);

        var result = await _logic.DeleteMessageAsync(_orgId, id);

        result.Should().BeOfType<NotFound>();
    }
}