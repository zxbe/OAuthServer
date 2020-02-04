﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OAuthServer.Authorization.Repositories;
using OAuthServer.Presentation.Models;

namespace OAuthServer.Presentation.Controllers
{
    public class OAuthController : Controller
    {
        private readonly IClientRepository _clientRepository;

        public OAuthController(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        [HttpGet]
        [Route("OAuth/Authorize")]
        //public IActionResult AuthorizeLogin(string client_id, string response_type, string redirect_uri, string scope = "", string state = "")
        public async Task<IActionResult> AuthorizeLogin([FromQuery]AuthorizeModel model)
        {
            bool valid = ModelState.IsValid;

            var client = await _clientRepository.GetClientById(model.client_id);
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

            bool isConsentRequired = false;

            if (!isConsentRequired)
            {
                return await Authorize(model);
            }

            return View("Authorize", model);
        }

        [HttpPost]
        [Route("OAuth/Authorize")]
        public async Task<IActionResult> Authorize(AuthorizeModel model)
        {
            string authorization_code = "ABCDEF";

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

            if(model.Grant_Type != "authorization_code")
            {
                ModelState.AddModelError("grant_type", "unsupported grant type");

                return BadRequest(ModelState);
            }

            var response = new AccessTokenResponse()
            {
                Access_Token = "ABASDASDSADSADSAD",
                Scope = "my_scope",
                Token_Type = "bearer"
            };

            return Ok(response);
        }
    }
}