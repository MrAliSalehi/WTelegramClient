﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TL;

namespace WTelegramClientTest
{
	class Program_DownloadSavedMedia
	{
		static string Config(string what)
		{
			// go to Project Properties > Debug > Environment variables and add at least these: api_id, api_hash, phone_number
			if (what == "verification_code") { Console.Write("Code: "); return Console.ReadLine(); }
			return Environment.GetEnvironmentVariable(what);
		}

		static async Task Main(string[] args)
		{
			Console.WriteLine("The program will download photos/medias from messages you send/forward to yourself (Saved Messages)");
			using var client = new WTelegram.Client(Config);
			await client.ConnectAsync();
			var user = await client.LoginUserIfNeeded();
			client.Update += Client_Update;
			Console.ReadKey();

			async void Client_Update(ITLObject arg)
			{
				if (arg is not Updates { updates: var updates }) return;
				foreach (var update in updates)
				{
					if (update is not UpdateNewMessage { message: Message message } || message.peer_id.ID != user.ID)
						continue; // if it's not a new saved message, ignore it
					if (message.media is MessageMediaDocument { document: Document document })
					{
						int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from Mime to extension
						var filename = slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin";
						Console.WriteLine("Downloading " + filename);
						using var fileStream = File.Create(filename);
						await client.DownloadFileAsync(document, fileStream);
						Console.WriteLine("Download finished");
					}
					else if (message.media is MessageMediaPhoto { photo: Photo photo })
					{
						var filename = $"{photo.id}.jpg";
						Console.WriteLine("Downloading " + filename);
						using var fileStream = File.Create(filename);
						var type = await client.DownloadFileAsync(photo, fileStream);
						fileStream.Close(); // necessary for the renaming
						Console.WriteLine("Download finished");
						if (type is not Storage_FileType.unknown and not Storage_FileType.partial)
							File.Move(filename, Path.ChangeExtension(filename, type.ToString())); // rename extension
					}
				}
			}
		}
	}
}