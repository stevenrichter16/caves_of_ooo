using System;
using System.Net;
using System.Net.Mail;

namespace AiUnity.NLog.Core.Internal;

internal class MySmtpClient : SmtpClient, ISmtpClient, IDisposable
{
	public new void Dispose()
	{
	}

	string ISmtpClient.get_Host()
	{
		return base.Host;
	}

	void ISmtpClient.set_Host(string value)
	{
		base.Host = value;
	}

	int ISmtpClient.get_Port()
	{
		return base.Port;
	}

	void ISmtpClient.set_Port(int value)
	{
		base.Port = value;
	}

	int ISmtpClient.get_Timeout()
	{
		return base.Timeout;
	}

	void ISmtpClient.set_Timeout(int value)
	{
		base.Timeout = value;
	}

	ICredentialsByHost ISmtpClient.get_Credentials()
	{
		return base.Credentials;
	}

	void ISmtpClient.set_Credentials(ICredentialsByHost value)
	{
		base.Credentials = value;
	}

	bool ISmtpClient.get_EnableSsl()
	{
		return base.EnableSsl;
	}

	void ISmtpClient.set_EnableSsl(bool value)
	{
		base.EnableSsl = value;
	}

	void ISmtpClient.Send(MailMessage msg)
	{
		Send(msg);
	}
}
