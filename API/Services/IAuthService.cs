using API.DTOs;

namespace API.Services;

// Defines the contract for authentication.
// AuthController depends on this interface, not the concrete AuthService class.
// This means JWT logic can be swapped or tested independently of the controller.
public interface IAuthService
{
    // Returns a LoginResponse with a signed JWT on success.
    // Returns null if the credentials are invalid — the controller turns null into a 401.
    LoginResponse? Login(LoginRequest request);

    // Returns the full list of registered users — exposed only to Admin callers.
    IEnumerable<UserResponse> GetAllUsers();
}
