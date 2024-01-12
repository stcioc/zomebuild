mergeInto(LibraryManager.library,
{
	BrowserTextUpload: function (callbackObjectName, callbackMethodName, extensions) {
		// Strings received from C# must be decoded from UTF8
		FileCallbackObjectName = UTF8ToString(callbackObjectName);
		FileCallbackMethodName = UTF8ToString(callbackMethodName);
		var ext = UTF8ToString(extensions);
					
		// Create an input to take files if there isn't one already
		var fileuploader = document.getElementById('fileuploader');
		if (!fileuploader) {
			console.log('Creating fileuploader...');
			fileuploader = document.createElement('input');
			fileuploader.setAttribute('style', 'display:none;');
			fileuploader.setAttribute('type', 'file');
			fileuploader.setAttribute('id', 'fileuploader');
			fileuploader.setAttribute('class', 'nonfocused');
			// Set the received extensions
			if (ext !== "") {
				fileuploader.setAttribute('accept', ext);			
			}
			document.getElementsByTagName('body')[0].appendChild(fileuploader);

			fileuploader.onchange = function (event) {
				  var file = event.target.files[0];
				  var reader = new FileReader();
				  
				  reader.onload = function(e) {
					var fileName = file.name;
					var contents = e.target.result;
					
					console.log('ObjectName: ' + FileCallbackObjectName + ';\nMethodName: ' + FileCallbackMethodName + ';');
					var message = fileName + "|" + contents;
					SendMessage(FileCallbackObjectName, FileCallbackMethodName, message);
				  };
				  
				  reader.onerror = function() {
					SendMessage(FileCallbackObjectName, FileCallbackMethodName, "ERROR");
				  };
				  
				  if (file) {
					reader.readAsText(file);
				  }
			};
		}
		fileuploader.setAttribute('class', 'focused');
		fileuploader.click();
	},

   BrowserTextDownload: function(filename, textContent)
	{
		// https://ourcodeworld.com/articles/read/189/how-to-create-a-file-and-generate-a-download-with-javascript-in-the-browser-without-a-server
		
		// Convert paramters to the correct form. See Unity WebGL Plugins page
		// for more information. It's not too important to realize why you need 
		// to do this, as long as you know THAT you need to.
		var strFilename = UTF8ToString(filename);
		var strContent = UTF8ToString(textContent);

		// Create the hyperlink for a user to click
		var element = document.createElement('a');
		
		// Set the link destination as hard-coded file data.
		element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(strContent));
		element.setAttribute('download', strFilename);
		
		// Make sure it's not visible when added to the HTML body
		element.style.display = 'none'; 
		
		// Activate it by adding it to the HTML body
		document.body.appendChild(element);
		// Don't wait for the user to click it, activate it ourselves!
		element.click();
		// Clean up our mess, now that the anchor's purpose is finished.
		document.body.removeChild(element);
	},
	
	JavascriptConfirm: function(message)
	{
		var strMessage = UTF8ToString(message);
		if (confirm(strMessage)) {
			return 1;
		} else {
			return 0;
		} 
	},

	JavascriptPrompt: function(message, defaultText)
	{
		var strMessage = UTF8ToString(message);
		var strDefaultText = UTF8ToString(defaultText);
		let response = window.prompt(strMessage, strDefaultText);
		if (response == null) {
			response = "";
		}
		
		var bufferSize = lengthBytesUTF8(response) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(response, buffer, bufferSize);
		return buffer;		
	},
});