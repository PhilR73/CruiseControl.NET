﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using ThoughtWorks.CruiseControl.Remote.Messages;
using System.Reflection;
using System.IO;

namespace ThoughtWorks.CruiseControl.Remote
{
    /// <summary>
    /// Helper class for converting XML to objects.
    /// </summary>
    public static class XmlConversionUtil
    {
        #region Private fields
        private static Dictionary<string, Type> messageTypes = null;
        private static Dictionary<Type, XmlSerializer> messageSerialisers = new Dictionary<Type, XmlSerializer>();
        #endregion

        #region Public methods
        #region ProcessResponse()
        /// <summary>
        /// Converts a response string into a response object.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static Response ProcessResponse(string response)
        {
            // Find the type of message
            XmlDocument messageXml = new XmlDocument();
            messageXml.LoadXml(response);
            Type messageType = FindMessageType(messageXml.DocumentElement.Name);
            if (messageType == null)
            {
                throw new CommunicationsException(
                    string.Format(
                        "Unable to translate message: '{0}' is unknown",
                        messageXml.DocumentElement.Name));
            }

            // Convert the message and invoke the action
            object result = ConvertXmlToObject(messageType, response);
            return result as Response;
        }
        #endregion

        #region FindMessageType()
        /// <summary>
        /// Finds the type of object that a message is.
        /// </summary>
        /// <param name="messageName">The name of the message.</param>
        /// <returns>The message type, if found, null otherwise.</returns>
        public static Type FindMessageType(string messageName)
        {
            Type messageType = null;

            // If the message types have not been loaded, load them into a dictionary
            if (messageTypes == null)
            {
                messageTypes = new Dictionary<string, Type>();

                // Messages will only come from the communications library
                Assembly remotingLibrary = typeof(IServerConnection).Assembly;
                foreach (Type remotingType in remotingLibrary.GetExportedTypes())
                {
                    XmlRootAttribute[] attributes = remotingType.GetCustomAttributes(
                        typeof(XmlRootAttribute), false) as XmlRootAttribute[];
                    foreach (XmlRootAttribute attribute in attributes)
                    {
                        if (messageTypes.ContainsKey(attribute.ElementName))
                        {
                            throw new ApplicationException(
                                string.Format("Duplicate message type found: '{0}'.\r\nFirst type: {1}\r\nSecond type: {2}",
                                    attribute.ElementName,
                                    messageTypes[attribute.ElementName].FullName,
                                    remotingType.FullName));
                        }
                        else
                        {
                            messageTypes.Add(attribute.ElementName, remotingType);
                        }
                    }
                }
            }

            // Attempt to find the message within the message types
            if (messageTypes.ContainsKey(messageName))
            {
                messageType = messageTypes[messageName];
            }

            return messageType;
        }
        #endregion

        #region ConvertXmlToObject()
        /// <summary>
        /// Converts a message string into an object.
        /// </summary>
        /// <param name="messageType">The type of message.</param>
        /// <param name="message">The XML of the message.</param>
        /// <returns>The object of the message.</returns>
        public static object ConvertXmlToObject(Type messageType, string message)
        {
            object messageObj = null;

            // Make sure the serialiser has been loaded
            if (!messageSerialisers.ContainsKey(messageType))
            {
                messageSerialisers[messageType] = new XmlSerializer(messageType);
            }

            // Perform the actual conversion
            using (StringReader reader = new StringReader(message))
            {
                messageObj = messageSerialisers[messageType].Deserialize(reader);
            }

            return messageObj;
        }
        #endregion

        #region ConvertXmlToRequest()
        /// <summary>
        /// Converts a message string into a request.
        /// </summary>
        /// <param name="message">The XML of the message.</param>
        /// <returns>The converted <see cref="ServerRequest"/>.</returns>
        public static ServerRequest ConvertXmlToRequest(string message)
        {
            var messageXml = new XmlDocument();
            messageXml.LoadXml(message);
            var messageType = XmlConversionUtil.FindMessageType(messageXml.DocumentElement.Name);
            if (messageType == null) throw new CommunicationsException("Unknown message type");

            var request = ConvertXmlToObject(messageType, message) as ServerRequest;
            return request;
        }
        #endregion
        #endregion
    }
}
