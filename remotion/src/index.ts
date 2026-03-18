import { Composition } from "remotion";
import { MonadicSharpVideo } from "./compositions/MonadicSharpVideo";
import type { VideoProps } from "./types";

// ── Default props for Remotion Studio preview ────────────────────────────────
const defaultProps: VideoProps = {
  title: "Stop Writing try/catch — Use Result<T> Instead",
  screens: [
    {
      order: 1,
      label: "BEFORE — exception-driven",
      code: `public async Task<OrderDto> ProcessOrderAsync(int orderId)
{
    Order order;
    try
    {
        order = await _repo.GetOrderAsync(orderId);
        if (order == null)
            throw new NotFoundException($"Order {orderId} not found");
    }
    catch (NotFoundException) { throw; }
    catch (Exception ex)
    {
        throw new ServiceException("Database error", ex);
    }

    try
    {
        if (order.Status != OrderStatus.Pending)
            throw new InvalidStateException("Order must be Pending");

        await _inventory.ReserveAsync(order);
    }
    catch (InventoryException ex)
    {
        throw new ServiceException("Inventory failed", ex);
    }

    return new OrderDto(order);
}`,
      durationSeconds: 18,
    },
    {
      order: 2,
      label: "Result<T> — the honest signature",
      code: `public Result<User> CreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Bind(_ => ValidateEmail(request.Email))
        .Bind(_ => CheckEmailNotTaken(request.Email))
        .Map(_ => new User(request.Name, request.Email))
        .Bind(user => _repo.SaveAsync(user));
}

// Signature is honest — callers KNOW this can fail`,
      durationSeconds: 14,
    },
    {
      order: 3,
      label: "Map · Bind · Match",
      code: `// MAP — transform the value (next step cannot fail)
Result<string> formatted = Parse("42").Map(n => $"Value: {n}");

// BIND — chain fallible operations
Result<User> GetActiveUser(int id) =>
    FindUser(id)
        .Bind(ValidateActive)
        .Bind(LoadPermissions);

// MATCH — exit the railway at the boundary only
return result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(error.Message),
        _                    => Problem(error.Message)
    });`,
      durationSeconds: 20,
    },
    {
      order: 4,
      label: "AFTER — MonadicSharp pipeline",
      code: `// ✅ Same logic. 4 lines. Every failure typed and explicit.
public async Task<Result<OrderDto>> ProcessOrderAsync(int orderId)
{
    return await _repo.GetOrderAsync(orderId)
        .Bind(order => ValidatePending(order))
        .Bind(order => _inventory.ReserveAsync(order))
        .Map(order  => new OrderDto(order));
}`,
      durationSeconds: 16,
    },
  ],
  sections: [
    { sectionName: "hook",           durationSeconds: 28,  audioFile: "audio/hook.mp3" },
    { sectionName: "problema",       durationSeconds: 90,  audioFile: "audio/problema.mp3" },
    { sectionName: "soluzione",      durationSeconds: 120, audioFile: "audio/soluzione.mp3" },
    { sectionName: "building_blocks",durationSeconds: 120, audioFile: "audio/building_blocks.mp3" },
    { sectionName: "before_after",   durationSeconds: 90,  audioFile: "audio/before_after.mp3" },
    { sectionName: "cta",            durationSeconds: 30,  audioFile: "audio/cta.mp3" },
  ],
};

// ── Total duration in frames (30fps) ─────────────────────────────────────────
const totalSeconds = defaultProps.sections.reduce(
  (acc, s) => acc + s.durationSeconds, 0
);
const FPS = 30;

export const RemotionRoot: React.FC = () => (
  <Composition
    id="MonadicSharpVideo"
    component={MonadicSharpVideo}
    durationInFrames={totalSeconds * FPS}
    fps={FPS}
    width={1920}
    height={1080}
    defaultProps={defaultProps}
  />
);
