using System.Text.Json;

namespace Movie_API.Middleware
{
    public class ValidateMovieJsonFieldsMiddleware
    {
        private readonly RequestDelegate _next;
        public ValidateMovieJsonFieldsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
            {
                if (context.Request.ContentType != null && context.Request.ContentType.Contains("application/json"))
                {
                    context.Request.EnableBuffering();
                    using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                    {
                        var body = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;

                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            try
                            {
                                var jsonDoc = JsonDocument.Parse(body);
                                var root = jsonDoc.RootElement;
                                var missingFields = new List<string>();

                                // Cập nhật danh sách các trường bắt buộc cho Movie
                                string[] requiredFields = new string[]
                                {
                                    "Title",
                                    "Description",
                                    "IsWatched", // Đổi
                                    "DateWatched", // Đổi
                                    "Rating", // Đổi
                                    "Genre",
                                    "PosterUrl", // Đổi
                                    "DateAdded",
                                    "StudioID" // Đổi
                                };

                                foreach (var field in requiredFields)
                                {
                                    if (!root.TryGetProperty(field, out _))
                                    {
                                        missingFields.Add(field);
                                    }
                                }

                                if (missingFields.Count > 0)
                                {
                                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                    await context.Response.WriteAsJsonAsync(new
                                    {
                                        message = "Missing required fields",
                                        missingFields
                                    });
                                    return;
                                }
                            }
                            catch (JsonException)
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsJsonAsync(new { message = "Invalid JSON format" });
                                return;
                            }
                        }
                    }
                }
            }
            await _next(context);
        }
    }
}
