using DailyArena.Common.Core.Database;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace DailyArena.Common.Database
{
	/// <summary>
	/// Class that contains code for initializing WPF-specific delegates for the card database.
	/// </summary>
	public static class WpfInitializer
	{
		/// <summary>
		/// Initializes WPF-specific delegates for the card database.
		/// </summary>
		public static void InitializeDelegates()
		{
			CardDatabase.Protect = (data, salt) => ProtectedData.Protect(data, salt, DataProtectionScope.CurrentUser);

			CardDatabase.Unprotect = (data, salt) => ProtectedData.Unprotect(data, salt, DataProtectionScope.CurrentUser);

			Card.ImageUriResolver = c =>
			{
				Uri imageUri = null;

				string cachedImageLocation = $"{Card.CachedCardImageFolder}\\{c.ScryfallId}.jpg";
				Uri cachedImageUri = new Uri(cachedImageLocation);
				if (File.Exists(cachedImageLocation))
				{
					File.SetLastAccessTime(cachedImageLocation, DateTime.Now);
					imageUri = cachedImageUri;
				}
				else
				{
					using (WebClient client = new WebClient())
					{
						client.DownloadDataCompleted += (sender, e) =>
						{
							if (e.Error == null)
							{
								File.WriteAllBytes(cachedImageLocation, e.Result);
								imageUri = cachedImageUri;
								c.UpdateImageUriProperty(imageUri);
							}
							else
							{
								// this may be dfc...check dfc images
								try
								{
									byte[] front = client.DownloadData(new Uri($"https://www.jceddy.com/mtg/rmm/v2/card_images/normal/{c.ScryfallId}_0.jpg"));
									byte[] back = client.DownloadData(new Uri($"https://www.jceddy.com/mtg/rmm/v2/card_images/normal/{c.ScryfallId}_1.jpg"));

									using (MemoryStream frontStream = new MemoryStream(front))
									using (MemoryStream backStream = new MemoryStream(back))
									using (Image frontImage = Image.FromStream(frontStream))
									using (Image backImage = Image.FromStream(backStream))
									{
										int width = frontImage.Width + backImage.Width;
										int height = Math.Max(frontImage.Height, backImage.Height);
										float horizontalResolution = frontImage.HorizontalResolution;
										float verticalResolution = frontImage.VerticalResolution;

										using (Bitmap combinedImage = new Bitmap(width, height))
										{
											combinedImage.SetResolution(horizontalResolution, verticalResolution);
											using (Graphics g = Graphics.FromImage(combinedImage))
											{
												g.Clear(Color.White);
												g.DrawImage(frontImage, new Point(0, 0));
												g.DrawImage(backImage, new Point(frontImage.Width, 0));

												combinedImage.Save(cachedImageLocation, ImageFormat.Jpeg);
											}
										}
									}

									imageUri = cachedImageUri;
									c.UpdateImageUriProperty(imageUri);
								}
								catch (WebException) { /* ignore WebException...just means we didn't find the image */ }
							}
						};
						client.DownloadDataAsync(new Uri($"https://www.jceddy.com/mtg/rmm/v2/card_images/normal/{c.ScryfallId}.jpg"));
					}
				}

				return imageUri;
			};
		}
	}
}
