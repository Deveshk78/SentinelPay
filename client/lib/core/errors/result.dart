/// Minimal Result type so the domain layer needs no third-party packages.
sealed class Result<T> {
  const Result();
}
final class Ok<T> extends Result<T> {
  const Ok(this.value);
  final T value;
}
final class Err<T> extends Result<T> {
  const Err(this.message, {this.code});
  final String message;
  final int? code;
}
