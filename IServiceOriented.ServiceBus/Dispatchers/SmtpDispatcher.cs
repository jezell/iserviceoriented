using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.ServiceModel.Channels;
using System.IO;
using System.Xml;
using System.Globalization;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    public class SmtpDispatcher : Dispatcher
    {
        public SmtpDispatcher()
        {
        }

        public SmtpDispatcher(string subject, MailAddress from, MailAddress[] to)
        {
            _subject = subject;
            _from = from;
            _to = to;
        }

        string _subject;
        MailAddress _from;
        MailAddress[] _to;

        MessageDeliveryConverter _converter;
        public MessageDeliveryConverter Converter
        {
            get
            {
                if (_converter == null)
                {
                    _converter = MessageDeliveryConverter.CreateConverter(Endpoint.ContractType);
                }
                return _converter;
            }
        }

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            try
            {
                SmtpClient client = new SmtpClient();
                MailMessage mailMessage = new MailMessage();
                mailMessage.Subject = DetermineSubject(messageDelivery);
                mailMessage.From = DetermineFromAddress(messageDelivery);

                foreach (MailAddress address in DetermineToEmailAddresses(messageDelivery))
                {
                    mailMessage.To.Add(address);
                }

                foreach (MailAddress address in DetermineCCEmailAddresses(messageDelivery))
                {
                    mailMessage.CC.Add(address);
                }

                foreach (MailAddress address in DetermineBccEmailAddresses(messageDelivery))
                {
                    mailMessage.Bcc.Add(address);
                }
                mailMessage.BodyEncoding = BodyEncoding;

                Message content = Converter.ToMessage(messageDelivery);

                using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    using (XmlTextWriter xmlWriter = new XmlTextWriter(writer))
                    {
                        content.WriteMessage(xmlWriter);
                    }

                    mailMessage.Body = writer.ToString();
                }

                client.Send(mailMessage);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                throw new DeliveryException("Error sending mail", ex);
            }
        }

        public Encoding BodyEncoding
        {
            get;
            set;
        }

        public virtual string DetermineSubject(MessageDelivery messageDelivery)
        {
            return _subject;
        }

        public virtual MailAddress DetermineFromAddress(MessageDelivery messageDelivery)
        {
            return _from;
        }
        
        public virtual IEnumerable<MailAddress> DetermineToEmailAddresses(MessageDelivery messageDelivery)
        {
            return _to;
        }
        
        public virtual IEnumerable<MailAddress> DetermineCCEmailAddresses(MessageDelivery messageDelivery)
        {
            return new MailAddress[0];
        }
        public virtual IEnumerable<MailAddress> DetermineBccEmailAddresses(MessageDelivery messageDelivery)
        {
            return new MailAddress[0];
        }
        
    }
}
