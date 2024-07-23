namespace Gateway.Middlewares;

public class InterceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers["Referrer"] = "Api-Gateway";
        await next(context);
    }
}
