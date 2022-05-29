﻿#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BricklinkSharp.Client.OAuth;

namespace BricklinkSharp.Client.Extensions;

internal static class HttpClientExtensions
{
    private static void GetAuthorizationHeader(string url, string method,
        out string scheme, out string parameter)
    {
        BricklinkClientConfiguration.Instance.ValidateThrowException();

        var request = new OAuthRequest(BricklinkClientConfiguration.Instance.ConsumerKey!,
            BricklinkClientConfiguration.Instance.ConsumerSecret!,
            BricklinkClientConfiguration.Instance.TokenValue!,
            BricklinkClientConfiguration.Instance.TokenSecret!,
            url,
            method);

        var header = request.GetAuthorizationHeader();
        var schemeParameter = header.Split(' ');
        scheme = schemeParameter[0];
        parameter = schemeParameter[1];
    }

    public static async Task<string> ExecuteRequest(this HttpClient httpClient,string url, 
        HttpMethod httpMethod, object? body = null, 
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        GetAuthorizationHeader(url, httpMethod.ToString(), out var authScheme, out var authParameter);
        using var message = new HttpRequestMessage(httpMethod, url);
        message.Headers.Authorization = new AuthenticationHeaderValue(authScheme, authParameter);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, options);
            using var content = new StringContent(json, Encoding.Default, "application/json");
            content.Headers.ContentType.CharSet = string.Empty;
            message.Content = content;
        }
        else
        {
            message.Content = null;
        }

        var response = await httpClient.SendAsync(message, cancellationToken);

#if HAVE_HTTP_CONTENT_READ_CANCELLATION_TOKEN
            var contentAsString = await response.Content.ReadAsStringAsync(cancellationToken);
#else
        var contentAsString = await response.Content.ReadAsStringAsync();
#endif
        return contentAsString;
    }
}