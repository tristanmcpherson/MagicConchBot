using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net.Mime;

namespace MagicConchBot.App.Controllers {
	[Route("api/[controller]")]
	public class UploadController : Controller {
		private readonly IDictionary<Guid, string> mFileDictionary;
		private readonly IDictionary<string, Guid> mReverseDictionary;

		public UploadController(IDictionary<Guid, string> fileDictionary, IDictionary<string, Guid> reverseDictionary) {
			this.mFileDictionary = fileDictionary;
			this.mReverseDictionary = reverseDictionary;
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetDataFileResponse(Guid? id) {
			try {
				if (id == null) {
					return NotFound();
				}
				var guid = (Guid)id;
				var fileName = mFileDictionary.ContainsKey(guid) ? mFileDictionary[guid] : null;
				var filePath = Path.Combine(Constants.LocalPath, fileName);

				if (fileName == null) {
					return NotFound();
				}

				var cd = new ContentDisposition {
					FileName = HttpUtility.UrlEncode(fileName),
					Inline = false,
				};

				Response.Headers.Add("Content-Disposition", cd.ToString());

				var file = await System.IO.File.ReadAllBytesAsync(filePath);
				return File(file, "audio/mpeg");

			} catch (Exception e) {
				Console.WriteLine(e);
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}

		[HttpPost]
		public async Task<IActionResult> UploadFile(IFormFile file) {
			try {
				using (var uploadFileStream = file.OpenReadStream()) {
					using (var buffer = new MemoryStream()) {
						await uploadFileStream.CopyToAsync(buffer);

						using (var md5 = MD5.Create()) {
							var hash = md5.ComputeHash(buffer);
							var hashString = BitConverter.ToString(hash).Replace("-", "");
							var extension = file.FileName.Split('.').Last();
							var fullFileName = $"{file.FileName.Split('.').First()}-{hashString}.{extension}";
							var filePath = Path.Combine(Constants.LocalPath, fullFileName);

							if (!Directory.Exists(Constants.LocalPath)) {
								Directory.CreateDirectory(Constants.LocalPath);
							}

							if (mReverseDictionary.ContainsKey(fullFileName)) {
								return Ok(mReverseDictionary[fullFileName]);
							}

							using (var localFileStream = new FileStream(filePath, FileMode.Create)) {
								await file.CopyToAsync(localFileStream);
							}

							var guid = Guid.NewGuid();
							mFileDictionary[guid] = fullFileName;
							mReverseDictionary[fullFileName] = guid;

							return Ok(guid);
						}
					}
				}   
			} catch (Exception ex) {
				// Get better logging
				Console.WriteLine(ex.ToString());
				return StatusCode(500, "Failed to upload file. " + ex);
			}
		}
	}
}
