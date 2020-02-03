﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OAuthServer.Presentation.Models;

namespace OAuthServer.Presentation.Controllers
{
    public class OAuthController : Controller
    {
        [HttpGet]
        [Route("OAuth/Authorize")]
        //public IActionResult AuthorizeLogin(string client_id, string response_type, string redirect_uri, string scope = "", string state = "")
        public IActionResult AuthorizeLogin([FromQuery]AuthorizeModel model)
        {
            bool valid = ModelState.IsValid;

            if(model.response_type != "code")
            {
                //ModelState.AddModelError("")
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
        public async Task<IActionResult> GetAccessToken()
        {
            return Ok();
        }
    }
}