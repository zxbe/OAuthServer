﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuthServer.Application;
using OAuthServer.Authorization.EntityFramework;
using OAuthServer.Authorization.Models;
using OAuthServer.Authorization.Repositories;
using OAuthServer.Presentation.Models;
using OAuthServer.Util;

namespace OAuthServer.Presentation.Controllers
{
    public class OAuthController : Controller
    {

        private readonly AuthUnitOfWork<User> _authUnitOfWork;
        //private readonly IClientRepository _clientRepository;
        //private readonly IConsentRepository<User> _consentRepository;
        private readonly IAuthorizationCodeRepository<User> _authorizationCodeRepository;
        private readonly JwtSecurityTokenHelper _tokenHelper;

        public OAuthController(AuthUnitOfWork<User> authUnitOfWork, 
            //IClientRepository clientRepository, IConsentRepository<User> consentRepository, 
            IAuthorizationCodeRepository<User> authorizationCodeRepository, JwtSecurityTokenHelper tokenHelper)
        {
            _authUnitOfWork = authUnitOfWork;
            //_clientRepository = clientRepository;
            //_consentRepository = consentRepository;
            _authorizationCodeRepository = authorizationCodeRepository;
            _tokenHelper = tokenHelper;

        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet]
        [Route("OAuth/Authorize")]
        //public IActionResult AuthorizeLogin(string client_id, string response_type, string redirect_uri, string scope = "", string state = "")
        public async Task<IActionResult> AuthorizeLogin([FromQuery]AuthorizeModel model)
        {
            bool valid = ModelState.IsValid;

            var username = "3ef7fb91-7dd6-4a35-b9a9-99d5fec053a8";

            var client = await _authUnitOfWork.ClientRepository.GetClientById(model.client_id);
            if(client == null)
            {
                return BadRequest("Unrecognized client_id");
            }

            if(client.Redirect_Uri != model.redirect_uri)
            {
                return BadRequest("Invalid redirect URI");
            }

            if(model.response_type != "code")
            {
                //ModelState.AddModelError("")
            }

            var consent = await _authUnitOfWork.ConsentRepository.GetUserConsentByClientId(model.client_id, username);

            bool isConsentRequired = true;

            if (consent != null)
                isConsentRequired = false;

            if (!isConsentRequired)
            {
                return await Authorize(model);
            }

            return View("Authorize", model);
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("OAuth/Authorize")]
        public async Task<IActionResult> Authorize(AuthorizeModel model)
        {
            var username = "3ef7fb91-7dd6-4a35-b9a9-99d5fec053a8";

            var consent = await _authUnitOfWork.ConsentRepository.GetUserConsentByClientId(model.client_id, username);

            if (consent == null)
            {
                consent = consent ?? new Consent<User>()
                {
                    Client_Id = model.client_id,
                    User_Id = username,
                    Scope = model.scope
                };
                _authUnitOfWork.ConsentRepository.AddConsent(consent);
                await _authUnitOfWork.SaveAsync();
            }

            var existingcode = await _authorizationCodeRepository.GetAuthorizationCodeByUserId(model.client_id, username);
            if(existingcode != null)
            {
                //_authorizationCodeRepository.RemoveRange(new List<AuthorizationCode>() { existingcode });
                existingcode.Expired = true;
            }

            var bytes = new byte[16];
            new Random().NextBytes(bytes);
            string hex = BitConverter.ToString(bytes).Replace("-", string.Empty);

            var authCode = new AuthorizationCode<User>(hex, consent, DateTime.Now.AddMinutes(5));
            _authorizationCodeRepository.AddAuthorizationCode(authCode);


            string authorization_code = authCode.Code;

             var redirection_path = model.redirect_uri + "?code=" + authorization_code;
            if (!string.IsNullOrEmpty(model.state))
                redirection_path += "&state=" + model.state;

            return Redirect(redirection_path);
        }

        [HttpPost]
        [Route("OAuth/Token")]
        public async Task<IActionResult> GetAccessToken([FromBody] AccessTokenRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = "3ef7fb91-7dd6-4a35-b9a9-99d5fec053a8";

            if (model.Grant_Type != "authorization_code")
            {
                ModelState.AddModelError("grant_type", "unsupported grant type");

                return BadRequest(ModelState);
            }

            // validate client
            var client = await _authUnitOfWork.ClientRepository.GetClientById(model.Client_Id);
            if (client.Client_Secret != model.Client_Secret)
                return BadRequest("Invalid Client Information");

            // get code
            var code = await _authorizationCodeRepository.GetAuthorizationCodeByCode(model.Code);
            if (code == null || code.Expired || code.Expiry <= DateTime.Now)
                return BadRequest("Code Invalid or Expired");

            //invalidate Code after use
            _authorizationCodeRepository.InvalidateCode(code);

            var jwtToken = _tokenHelper.GenerateJwtToken(new List<System.Security.Claims.Claim>() {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, username),
                new System.Security.Claims.Claim("cid", client.Client_Id),
            });

            var response = new AccessTokenResponse()
            {
                Access_Token = jwtToken,
                Scope = "my_scope",
                Token_Type = "bearer"
            };

            return Ok(response);
        }
    }
}