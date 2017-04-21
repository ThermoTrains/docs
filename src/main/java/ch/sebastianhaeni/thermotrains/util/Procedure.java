package ch.sebastianhaeni.thermotrains.util;

/**
 * A procedure is basically a {@link Runnable} but it can throw an exception.
 * @param <T> exception thrown
 */
@FunctionalInterface
public interface Procedure<T extends Exception> {
  void run() throws T;
}
