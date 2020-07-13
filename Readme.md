### Introduction

This repository contains an easy-to-use and well-documented .NET assembly for communicating with
an XMPP server. It supports basic Instant Messaging and Presence funtionality as well as a variety
of XMPP extensions.

![Project Health](https://github.com/luz-sf/Net.Xmpp/workflows/Project%20Health/badge.svg)

### Supported XMPP Features

The library fully implements the [XMPP Core](http://xmpp.org/rfcs/rfc3920.html) and 
[XMPP IM](http://xmpp.org/rfcs/rfc3921.html) specifications and thusly provides the basic XMPP instant
messaging (IM) and presence functionality. In addition, the library offers support for most of the
optional procotol extensions. More specifically, the following features are supported:

+ [XEP-0004](https://xmpp.org/extensions/xep-0004.html) Data Forms
+ [XEP-0020](https://xmpp.org/extensions/xep-0020.html) Feature Negotiation
+ [XEP-0030](https://xmpp.org/extensions/xep-0030.html) Service Discovery
+ [XEP-0045](https://xmpp.org/extensions/xep-0045.html) Multi-User Chat
+ [XEP-0047](https://xmpp.org/extensions/xep-0047.html) In-Band Bytestreams
+ [XEP-0055](https://xmpp.org/extensions/xep-0055.html) Jabber Search
+ [XEP-0059](https://xmpp.org/extensions/xep-0059.html) Result Set Management
+ [XEP-0065](https://xmpp.org/extensions/xep-0065.html) SOCKS5 Bytestreams
+ [XEP-0077](https://xmpp.org/extensions/xep-0077.html) In-Band Registration
+ [XEP-0082](https://xmpp.org/extensions/xep-0082.html) XMPP Date and Time Profiles
+ [XEP-0084](https://xmpp.org/extensions/xep-0084.html) User Avatar
+ [XEP-0085](https://xmpp.org/extensions/xep-0085.html) Chat State Notifications
+ [XEP-0092](https://xmpp.org/extensions/xep-0092.html) Software Version
+ [XEP-0095](https://xmpp.org/extensions/xep-0095.html) Stream Initiation
+ [XEP-0096](https://xmpp.org/extensions/xep-0095.html) SI File Transfer
+ [XEP-0107](https://xmpp.org/extensions/xep-0107.html) User Mood
+ [XEP-0108](https://xmpp.org/extensions/xep-0108.html) User Activity
+ [XEP-0115](https://xmpp.org/extensions/xep-0115.html) Entity Capabilities
+ [XEP-0118](https://xmpp.org/extensions/xep-0118.html) User Tune
+ [XEP-0136](https://xmpp.org/extensions/xep-0136.html) Message Archiving
+ [XEP-0153](https://xmpp.org/extensions/xep-0153.html) vCard-Based Avatars
+ [XEP-0163](https://xmpp.org/extensions/xep-0163.html) Personal Eventing Protocol
+ [XEP-0191](https://xmpp.org/extensions/xep-0191.html) Blocking Command
+ [XEP-0199](https://xmpp.org/extensions/xep-0199.html) XMPP Ping
+ [XEP-0202](https://xmpp.org/extensions/xep-0202.html) Entity Time
+ [XEP-0203](https://xmpp.org/extensions/xep-0203.html) Delayed Delivery
+ [XEP-0224](https://xmpp.org/extensions/xep-0224.html) Attention
+ [XEP-0231](https://xmpp.org/extensions/xep-0231.html) Bits of Binary
+ [XEP-0279](https://xmpp.org/extensions/xep-0279.html) Server IP Check
+ [XEP-0280](https://xmpp.org/extensions/xep-0280.html) Message Carbons
+ [XEP-0313](https://xmpp.org/extensions/xep-0313.html) Message Archive Management
+ [XEP-0363](https://xmpp.org/extensions/xep-0363.html) HTTP Upload File



+ Simplified Blocking
+ API designed to be very easy to use
+ Well documented with lots of example code
+ Free to use in commercial and personal projects (MIT License)


### Where to get it

You can always get the latest binary package on [Nuget](http://www.nuget.org/packages/S22.Xmpp) or
download the binaries as a .zip archive from [GitHub](http://smiley22.github.com/Downloads/S22.Xmpp.zip). 
The [documentation](http://smiley22.github.com/S22.Xmpp/Documentation/) is also available for offline viewing 
as HTML or CHM and can be downloaded from 
[here](http://smiley22.github.com/Downloads/S22.Xmpp.Html.Documentation.zip) and 
[here](http://smiley22.github.com/Downloads/S22.Xmpp.Chm.Documentaton.zip), respectively.


### Usage & Examples

To use the library add the S22.Xmpp.dll assembly to your project references in Visual Studio. Here's
a simple example that initializes a new instance of the XmppClient class and connects to an XMPP
server:

	using System;
	using Net.Xmpp;
	using Net.Xmpp.Client;

	namespace Test {
		class Program {
			static void Main(string[] args) {
				/* connect on port 5222 using TLS/SSL if available */
				using (var client = new XmppClient("jabber.se", "username", "password"))
				{
					Console.WriteLine("Connected as " + client.Jid);
				}
			}
		}
	}

Please see the [documentation](http://smiley22.github.com/S22.Xmpp/Documentation/) for a getting started
guide, examples and details on using the classes and methods exposed by the S22.Xmpp assembly.


### Credits

The Net.Xmpp library is copyright © 2020 LUZ Soluções Financeiras.
The previous Sharp.Xmpp library is copyright © 2015 Panagiotis Georgiou Stathopoulos.
The initial S22.Xmpp library is copyright © 2013-2014 Torben Könke.


### License

This library is released under the [MIT license](https://github.com/pgstath/Sharp.Xmpp/blob/master/License.md).
