using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sabio.Models;
using Sabio.Models.Domain.Files;
using Sabio.Models.Requests.Files;
using Sabio.Services;
using Sabio.Web.Controllers;
using Sabio.Web.Models.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sabio.Web.Api.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FileApiController : BaseApiController
    {
        private IFilesService _service = null;
        private IAuthenticationService<int> _authService = null;

        public FileApiController(IFilesService service
          , ILogger<FileApiController> logger
          , IAuthenticationService<int> authService) : base(logger)
        {
            _service = service;
            _authService = authService;
        }
        [HttpDelete("{id:int}")]
        public ActionResult<SuccessResponse> Delete(int id)
        {
            int code = 200;
            BaseResponse response = null;//do not declare an instance.

            try
            {
                _service.Delete(id);
                response = new SuccessResponse();
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
            }
            return StatusCode(code, response);
        }
        [HttpGet("{id:int}")]
        public ActionResult<ItemResponse<File>> Get(int id)
        {
            int iCode = 200;
            BaseResponse response = null;
            
            try
            {
                File file = _service.Get(id);

                if (file == null)
                {
                    iCode = 404;
                    response = new ErrorResponse("Application Resource not found.");
                }
                else
                {
                    response = new ItemResponse<File> { Item = file };
                }
            }
            catch (Exception ex)
            {
                iCode = 500;
                base.Logger.LogError(ex.ToString());
                response = new ErrorResponse($"Generic Error: {ex.Message}");
            }
            return StatusCode(iCode, response);
        }
        [HttpGet("search")]
        public ActionResult<ItemResponse<Paged<File>>> GetPageSearch(int pageIndex, int pageSize, int createdBy)
        {
            int code = 200;
            BaseResponse response = null;//do not declare an instance.
            try
            {
                Paged<File> page = _service.GetPageCreatedBy(pageIndex, pageSize, createdBy);

                if (page == null)
                {
                    code = 404;
                    response = new ErrorResponse("App Resource not found.");
                }
                else
                {
                    response = new ItemResponse<Paged<File>> { Item = page };
                }
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
                base.Logger.LogError(ex.ToString());
            }
            return StatusCode(code, response);
        }
        [HttpGet("paginate")]
        public ActionResult<ItemResponse<Paged<File>>> GetPage(int pageIndex, int pageSize)
        {
            int code = 200;
            BaseResponse response = null;//do not declare an instance.
            try
            {
                Paged<File> page = _service.GetPage(pageIndex, pageSize);

                if (page == null)
                {
                    code = 404;
                    response = new ErrorResponse("App Resource not found.");
                }
                else
                {
                    response = new ItemResponse<Paged<File>> { Item = page };
                }
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
                base.Logger.LogError(ex.ToString());
            }
            return StatusCode(code, response);
        }
        [HttpPut("{id:int}")]
        public ActionResult<SuccessResponse> Update(FileUpdateRequest model)
        {
            int code = 200;
            BaseResponse response = null;//

            try
            {
                _service.Update(model);

                response = new SuccessResponse();
            }
            catch (Exception ex)
            {
                code = 500;
                response = new ErrorResponse(ex.Message);
            }
            return StatusCode(code, response);
        }
        [HttpPost] 
        public IActionResult UploadAsync(List<IFormFile> formFiles)
        {
            try
            {
                int id = _authService.GetCurrentUserId();

                List<FileBase> fileResponseList = new List<FileBase>();
                foreach (IFormFile file in formFiles)
                {
                    FileBase fileBase = _service.UploadAsync(file, id);

                    fileResponseList.Add(fileBase);
                }
                return Ok(fileResponseList);
            }
            catch (Exception ex)
            {
                base.Logger.LogError(ex.ToString());
                return BadRequest($"Generic Error: {ex.Message}");
            }
        }
    }
}
