namespace FeedService.Exceptions;

public class NotFoundException : Exception { public NotFoundException(string msg) : base(msg) { } }
public class UnauthorizedException : Exception { public UnauthorizedException(string msg = "Unauthorized") : base(msg) { } }