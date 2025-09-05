using System;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Text;
using BT;


namespace BT.SaaS.IspssAdapter
{
	/// <summary>
	/// Encrypt/Decrypt the input data using Blowfish algorithm.
	/// </summary>
	public sealed class BlowfishEncryptor
	{
        private BlowfishEncryptor()
        { }

		# region Encrypt
		/// <summary>
		/// Encrypts the input string
		/// </summary>
		/// <param name="inputData"></param>
		/// <returns>string</returns>
		public static string Encrypt(string inputData)
		{
			Blowfish blowFish;
            string keyString;

			try
			{
                keyString = ConfigurationManager.AppSettings["blowfishKeyString"];

				blowFish = new Blowfish(keyString);

				return blowFish.encipher(inputData.Trim());
			}
			catch (Exception ex)
			{
				throw new MappingException("Unable to encrypt password - " + ex.Message);
			}
			finally
			{
				keyString = null;
				blowFish = null;
			}
		}
		# endregion

  		# region Decrypt
		/// <summary>
		/// Decrypts the input string
		/// </summary>
		/// <param name="inputData"></param>
		/// <returns>string</returns>
		public static string Decrypt(string inputData)
		{
			string keyString;
			Blowfish blowFish;

			try
			{
                keyString = ConfigurationManager.AppSettings["blowfishKeyString"];

				blowFish = new Blowfish(keyString);

				return blowFish.decipher(inputData).Replace("\0", string.Empty);
			}
			catch (Exception ex)
			{
				throw new MappingException("Unable to decrypt password - " + ex.Message);
			}
			finally
			{
				keyString = null;
				blowFish = null;
			}
		}
		# endregion
	}
}
