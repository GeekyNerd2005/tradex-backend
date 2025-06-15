using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace tradex_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public MarketController()
    {
        _httpClient = new HttpClient();
    }

    [HttpGet("price")]
    public async Task<IActionResult> GetPrice([FromQuery] string ticker)
    {
        if (string.IsNullOrEmpty(ticker))
            return BadRequest(new { error = "Ticker is required" });

        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5050/price?ticker={ticker}");
            request.Headers.Add("Authorization", token); // Forward the JWT

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { error = $"Failed to fetch price: {response.StatusCode}" });

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(content);

            return Ok(data.RootElement);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Server error", detail = ex.Message });
        }
    }

 [HttpGet("price-history/{ticker}")]
public async Task<IActionResult> GetHistoricalData(string ticker, [FromQuery] string interval = "1d")
{
    try
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval={interval}&range=max";

        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Failed to fetch chart data");

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}




   [HttpGet("candles/{ticker}")]
public async Task<IActionResult> GetCandlestickData(string ticker, [FromQuery] string range = "max", [FromQuery] string interval = "1d")
{
    try
    {
        using var client = new HttpClient();
         client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = $"http://localhost:8000/candles/{ticker}?range={range}&interval={interval}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error: {ex.Message}");
    }
}

private (string, string) MapRangeToIntervalAndPeriod(string range)
{
    return range switch
    {
        "1d" => ("1m", "1d"),
        "5d" => ("5m", "5d"),
        "1mo" => ("1h", "1mo"),
        "3mo" => ("2h", "3mo"),
        "6mo" => ("1d", "6mo"),
        "1y" => ("1d", "1y"),
        "5y" => ("1wk", "5y"),
        "max" => ("1mo", "max"),
        _ => ("1d", "3mo"),
    };
}


}





