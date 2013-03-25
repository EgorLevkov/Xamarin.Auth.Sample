using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Xamarin.Auth.Sample.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		private readonly TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

		UIWindow window;
		DialogViewController dialog;
		
		Section facebook;
		StringElement facebookStatus;
		
		Section vkontakte;
		StringElement vkontakteStatus;

		Section odnoklasniki;
		StringElement odnoklasnikiStatus;

		private void LoginToFacebook ()
		{
			var auth = new OAuth2Authenticator (
				clientId: "475933549130692",
				scope: "",
				authorizeUrl: new Uri ("https://m.facebook.com/dialog/oauth/"),
				redirectUrl: new Uri ("http://www.facebook.com/connect/login_success.html"));

			auth.Completed += FacebookAuthorizationCompleted;

			UIViewController vc = auth.GetUI ();
			dialog.PresentViewController (vc, true, null);
		}

		private void FacebookAuthorizationCompleted(object sender, AuthenticatorCompletedEventArgs e)
		{
			dialog.DismissViewController (true, null);
			
			if (!e.IsAuthenticated) {
				facebookStatus.Caption = "Not authorized";
				dialog.ReloadData();
				return;
			}

			String accessToken = String.Empty;
			e.Account.Properties.TryGetValue("access_token", out accessToken);

			var request = new OAuth2Request ("GET", new Uri ("https://graph.facebook.com/me"), null, e.Account);
			request.GetResponseAsync().ContinueWith (t =>
         	{
				if (t.IsFaulted)
					facebookStatus.Caption = "Error: " + t.Exception.InnerException.Message;
				else if (t.IsCanceled)
					facebookStatus.Caption = "Canceled";
				else
				{
					//Parse Json result

					var obj = JsonValue.Parse (t.Result.GetResponseText());
					facebookStatus.Caption = "Logged in as " + obj["name"];
				}
				
				dialog.ReloadData();
			},
			uiScheduler);
		}

		private void LoginToVK()
		{
			var auth = new OAuth2Authenticator (
				clientId: "3508461",
				scope: "",
				authorizeUrl: new Uri ("https://oauth.vk.com/authorize"),
				redirectUrl: new Uri ("http://oauth.vk.com/blank.html"));

			auth.Completed += VkontakteAuthorizationCompleted;
		
			UIViewController vc = auth.GetUI ();
			dialog.PresentViewController (vc, true, null);
		}

		private void VkontakteAuthorizationCompleted(object sender, AuthenticatorCompletedEventArgs e)
		{
			dialog.DismissViewController (true, null);
			
			if (!e.IsAuthenticated)
			{
				vkontakteStatus.Caption = "Not authorized";
				dialog.ReloadData();
				return;
			}
			
			String accessToken = String.Empty;
			e.Account.Properties.TryGetValue("access_token", out accessToken);
			
			String userId = String.Empty;
			e.Account.Properties.TryGetValue("user_id", out userId);
			
			String uri = String.Format("https://api.vk.com/method/users.get?uid={0}&access_token={1}", userId, accessToken);
			
			var request = new OAuth2Request ("GET", new Uri (uri), null, e.Account);
			request.GetResponseAsync().ContinueWith (t => {
				if (t.IsFaulted)
					vkontakteStatus.Caption = "Error: " + t.Exception.InnerException.Message;
				else if (t.IsCanceled)
					vkontakteStatus.Caption = "Canceled";
				else
				{
					//Parse Json result

					var obj = JsonObject.Parse (t.Result.GetResponseText());
					var resp = obj["response"] as JsonArray;
					vkontakteStatus.Caption = "Logged in as " + resp.FirstOrDefault()["first_name"];
				}
				
				dialog.ReloadData();
			}, uiScheduler);
		
		}

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			facebook = new Section ("Facebook");
			facebook.Add (new StyledStringElement ("Log in", LoginToFacebook));
			facebook.Add (facebookStatus = new StringElement ("Not authorized"));

			vkontakte = new Section("VKontakte");
			vkontakte.Add (new StyledStringElement("Log in", LoginToVK)); 
			vkontakte.Add(vkontakteStatus = new StringElement("Not authorized"));

			dialog = new DialogViewController (new RootElement ("Xamarin.Auth Sample") {
				facebook, vkontakte, odnoklasniki
			});

			window = new UIWindow (UIScreen.MainScreen.Bounds);
			window.RootViewController = new UINavigationController (dialog);
			window.MakeKeyAndVisible ();
			
			return true;
		}


		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
}

