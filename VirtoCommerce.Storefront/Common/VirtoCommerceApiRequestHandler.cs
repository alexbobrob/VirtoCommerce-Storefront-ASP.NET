﻿using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Storefront.Model;

namespace VirtoCommerce.Storefront.Common
{
    public class VirtoCommerceApiRequestHandler : ServiceClientCredentials
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly StorefrontOptions _options;

        public VirtoCommerceApiRequestHandler(IOptions<StorefrontOptions> options, IWorkContextAccessor workContextAccessor)
        {
            _options = options.Value;
            _workContextAccessor = workContextAccessor;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AddAuthorization(request);
            AddCurrentUser(request);

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }


        private void AddAuthorization(HttpRequestMessage request)
        {
            if (_options.Api != null)
            {
                var signature = new ApiRequestSignature { AppId = _options.Api.AppId };

                var parameters = new[]
                {
                    new NameValuePair(null, _options.Api.AppId),
                    new NameValuePair(null, signature.TimestampString)
                };

                signature.Hash = HmacUtility.GetHashString(key => new HMACSHA256(key), _options.Api.SecretKey, parameters);

                request.Headers.Authorization = new AuthenticationHeaderValue("HMACSHA256", signature.ToString());
            }
        }

        private void AddCurrentUser(HttpRequestMessage request)
        {
            var worContex = _workContextAccessor.WorkContext;
            if (worContex != null)
            {
                var currentUser = worContex.CurrentCustomer;

                //Add special header with user name to each API request for future audit and logging
                if (currentUser != null && currentUser.IsRegisteredUser)
                {
                    var userName = currentUser.OperatorUserName;

                    if (string.IsNullOrEmpty(userName))
                    {
                        userName = currentUser.UserName;
                    }

                    if (!string.IsNullOrEmpty(userName))
                    {
                        request.Headers.Add("VirtoCommerce-User-Name", userName);
                    }
                }
            }
        }
    }
}
