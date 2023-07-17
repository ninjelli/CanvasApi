﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CanvasApi.Client.Accounts;
using CanvasApi.Client.AssignmentGroups;
using CanvasApi.Client.Enrollments;
using CanvasApi.Client.Extentions;
using CanvasApi.Client.OAuth2;
using CanvasApi.Client.Users;
using Newtonsoft.Json;
using TiberHealth.Serializer;
using System.Web;
using CanvasApi.Client.Submissions;
using CanvasApi.Client.Pages;
using CanvasApi.Client.Courses;
using CanvasApi.Client._Base;
using CanvasApi.Client.EnrollmentTerms;
using CanvasApi.Client.Admins;
using CanvasApi.Client.Modules;
using CanvasApi.Client.Section;
using CanvasApi.Client.Roles;

[assembly: InternalsVisibleTo("CanvasApi.Client.Test")]
namespace CanvasApi.Client
{
    public class CanvasApiClient : ICanvasApiClient, IDisposable
    {
        private HttpClient Client { get; }
        private IServiceProvider ServiceProvider { get; }

        private readonly Lazy<OAuth2Client> OAuth2Client;

        private readonly Lazy<AccountsClient> AccountsClient;
        private readonly Lazy<AdminApiClient> AdminClient;
        private readonly Lazy<AssignmentGroupsApiClient> AssignmentGroupsClient;
        private readonly Lazy<CourseApiClient> CoursesClient;
        private readonly Lazy<EnrollmentApiClient> EnrollmentClient;
        private readonly Lazy<EnrollmentTermsApiClient> EnrollmentTermsClient;
        private readonly Lazy<ModulesApiClient> ModulesClient;
        private readonly Lazy<PageApiClient> PagesClient;
        private readonly Lazy<RolesApiClient> RolesClient;
        private readonly Lazy<SectionApi> SectionClient;
        private readonly Lazy<SubmissionsApiClient> SubmissionsApiClient;
        private readonly Lazy<UsersClient> UsersClient;

        public PagingOptions DefaultPagingOptions { get; private set; }

        /// <summary>
        /// Constructor - builds Http Client based on Domain and Key
        /// </summary>
        /// <param name="canvasDomain">The domain address to the Canvas Instance</param>
        /// <param name="apiKey">The API Key defined on the Canvas Instance for this application</param>
        public CanvasApiClient(string canvasDomain, string apiKey, IServiceProvider serviceProvider) :
            this(new HttpClient().ConfigureCanvasApi(canvasDomain, apiKey), serviceProvider)
        {
        }

        /// <summary>
        /// Constructor for an already initialized HttpClient.
        /// Used for Dependency Injection (startup.cs)
        ///
        ///             this.Services.AddHttpClient<ICanvasApiClient, CanvasApiClient>(client =>
        ///                 client.ConfigureCanvasApi(CanvasDomain, ApiKey)
        ///             );
        /// 
        /// </summary>
        /// <param name="client">Http Client object already initialized to the Canvas API domain/root and headers preparred.</param>
        public CanvasApiClient(HttpClient client, IServiceProvider serviceProvider)
        {
            this.Client = client;
            this.ServiceProvider = serviceProvider;

            this.DefaultPagingOptions = new PagingOptions();

            // Build the Apis            
            this.OAuth2Client = this.SetLazy<OAuth2Client>();

            this.AccountsClient = this.SetLazy<AccountsClient>();
            this.AdminClient = this.SetLazy<AdminApiClient>();
            this.AssignmentGroupsClient = this.SetLazy<AssignmentGroupsApiClient>();
            this.CoursesClient = this.SetLazy<CourseApiClient>();

            this.EnrollmentClient = this.SetLazy<EnrollmentApiClient>();
            this.EnrollmentTermsClient = this.SetLazy<EnrollmentTermsApiClient>();
            this.ModulesClient = this.SetLazy<ModulesApiClient>();
            this.PagesClient = this.SetLazy<PageApiClient>();
            this.RolesClient = this.SetLazy<RolesApiClient>();
            this.SectionClient = this.SetLazy<SectionApi>();
            this.SubmissionsApiClient = this.SetLazy<SubmissionsApiClient>();
            this.UsersClient = this.SetLazy<Users.UsersClient>();
        }

        private Lazy<TConcrete> SetLazy<TConcrete>() where TConcrete : ApiClientBase =>
            this.SetLazy(() =>
            {
                var concrete = (TConcrete)Activator.CreateInstance(typeof(TConcrete), this);
                return concrete;
            });

        private Lazy<TType> SetLazy<TType>(Func<TType> factory) => new Lazy<TType>(factory);

        #region ICanvasApi
        public IOAuth2Api OAuth2 => this.OAuth2Client.Value;

        public IAccountsApi Accounts => this.AccountsClient.Value;
        public IAdminApiClient Admin => this.AdminClient.Value;
        public IAssignmentGroupsApiClient AssignmentGroups => this.AssignmentGroupsClient.Value;
        public ICourseApiClient Courses => this.CoursesClient.Value;
        public IEnrollmentApiClient Enrollments => this.EnrollmentClient.Value;
        public IEnrollmentTermsApiClient EnrollmentTerms => this.EnrollmentTermsClient.Value;
        public IModulesApiClient Modules => this.ModulesClient.Value;
        public IPageApiClient Pages => this.PagesClient.Value;
        public IRolesApiClient Roles => this.RolesClient.Value;
        public ISectionApi Sections => this.SectionClient.Value;
        public ISubmissionsApiClient Submissions => this.SubmissionsApiClient.Value;
        public IUsersApi Users => this.UsersClient.Value;

        public bool VerifyConfiguration()
        {
            return this.Client != null &&
                !string.IsNullOrWhiteSpace(this.Client.BaseAddress?.AbsolutePath) &&
                this.Client.DefaultRequestHeaders.Any(item => item.Key == "Authorization" && item.Value is string[] vals && vals[0].StartsWith("Bearer"));
        }
        #endregion

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        internal Task<IEnumerable<TResult>> PagableApiOperation<TResult>(HttpMethod verb, string url, PagingOptions pagingOptions = null) =>
                this.PagableApiOperation<TResult, object>(verb, url, null, pagingOptions);

        internal Task<IEnumerable<TResult>> PagableApiOperation<TResult, TApiResult>(
            HttpMethod verb,
            string url,
            PagingOptions pagingOptions,
            Func<TApiResult, IEnumerable<TResult>> factory
        ) =>
            this.PagableApiOperation<TResult, TApiResult, object>(verb, url, null, factory, pagingOptions);

        internal async Task<IEnumerable<TResult>> PagableApiOperation<TResult, TBody>(
            HttpMethod verb,
            string url,
            TBody body,
            PagingOptions pagingOptions = null,
            CancellationToken cancellationToken = default)
                where TBody : class
        {
            var pageLinks = new PageLinks((pagingOptions ?? this.DefaultPagingOptions)?.AddPagingUrl(url) ?? url);
            var initialBuffer = await this.ApiOperation<IEnumerable<TResult>, TBody>(verb, pageLinks.OriginalUrl, body, pageLinks, cancellationToken);
            return new PageableResult<TResult>(initialBuffer, this, pageLinks);
        }

        internal async Task<IEnumerable<TResult>> PagableApiOperation<TResult, TApiResult, TBody>(
            HttpMethod verb,
            string url,
            TBody body,
            Func<TApiResult, IEnumerable<TResult>> factory,
            PagingOptions pagingOptions = null,
            CancellationToken cancellationToken = default)
                where TBody : class
        {
            var pageLinks = new PageLinks((pagingOptions ?? this.DefaultPagingOptions)?.AddPagingUrl(url) ?? url);
            var apiResults = await this.ApiOperation<TApiResult, TBody>(verb, pageLinks.OriginalUrl, body, pageLinks, cancellationToken);

            return new PageableResult<TResult, TApiResult>(apiResults, this, pageLinks, factory);
        }


        internal Task<TResult> ApiOperation<TResult>(HttpMethod verb, string url) => this.ApiOperation<TResult, object>(verb, url, null);

        internal async Task<TResult> ApiOperation<TResult, TBody>(
            HttpMethod verb,
            string url,
            TBody body,
            PageLinks pageLinks = null,
            CancellationToken cancellationToken = default)
                where TBody : class
        {
            var httpMessage = this.GenerateHttpRequest(verb, url, body);

            var result = await this.Client
                .SendAsync(httpMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            
            if (result.IsSuccessStatusCode)
            {
                var content = await result.Content.ReadAsStringAsync();

                if (pageLinks != null) pageLinks.SetHeaders(result.Headers);

                await using var contentStream = await result.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(contentStream);
                using var reader = new JsonTextReader(streamReader);

                var serializedCollection = new JsonSerializer()
                {
                    ContractResolver = new DefaultNemesContractResolver()
                }.Deserialize<TResult>(reader);

                return serializedCollection ?? default(TResult);
            }

            throw result.ToException();
        }

        private HttpRequestMessage GenerateHttpRequest(HttpMethod method, string url) => this.GenerateHttpRequest<object>(method, url, null);

        private HttpRequestMessage GenerateHttpRequest<TBody>(HttpMethod method, string url, TBody body)
            where TBody : class
        {
            HttpContent messageContent = null;
            var requestUrl = url;
            if (body != null)
            {
                messageContent = FormDataSerializer.Serialize(body, options =>
                {
                    options.DefaultNameResolver = new PascalToSnakeResolver();
                    options.EnumNameResolver = new PascalToSnakeResolver();
                    options.DefaultDateFormat = "yyyy-MM-dd HH:mm:sszzz";
                });

                if (method == HttpMethod.Get && messageContent is MultipartFormDataContent multipartFormDataContent)
                {
                    foreach (var content in multipartFormDataContent)
                    {
                        var dataTask = content.ReadAsStringAsync();
                        dataTask.Wait();

                        if (!string.IsNullOrWhiteSpace(dataTask.Result))
                        {
                            var paramName = content.Headers.ContentDisposition.Name.Trim('"');
                            requestUrl += $"{(requestUrl.Contains('?') ? '&' : '?')}{paramName}={HttpUtility.UrlEncode(dataTask.Result)}";
                        }
                    }
                }
            }

            var httpMessage = new HttpRequestMessage(method, requestUrl);
            httpMessage.Content = messageContent;

            return httpMessage;
        }

    }
}
