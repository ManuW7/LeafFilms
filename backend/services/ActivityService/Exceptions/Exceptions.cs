namespace ActivityService.Exceptions;

public class NotFoundException : Exception { public NotFoundException(string msg) : base(msg) { } public NotFoundException(string e, object k) : base($"{e} '{k}' not found.") { } }
public class ConflictException : Exception { public ConflictException(string msg) : base(msg) { } }
public class ForbiddenException : Exception { public ForbiddenException(string msg = "Forbidden") : base(msg) { } }