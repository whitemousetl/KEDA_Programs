using CollectorService.CustomException;
using CollectorService.Models;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace CollectorService.Protocols;
public class OpcUaDriver : IProtocolDriver
{
    private Session? _conn;
    private string _protocolName = "OpcUaClient";

    public async Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        try
        {
            if (_conn == null)
            {
                var config = new ApplicationConfiguration()
                {
                    ApplicationName = "MyClient",
                    ApplicationUri = Utils.Format(@"urn:{0}:MyClient", System.Net.Dns.GetHostName()),
                    ApplicationType = ApplicationType.Client,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = "./CertificateStores/MachineDefault",
                            SubjectName = "MyClientSubjectName"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "./CertificateStores/UA Certificate Authorities"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "./CertificateStores/UA Applications"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "./CertificateStores/RejectedCertificates"
                        },
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        MinimumCertificateKeySize = 1024,
                        NonceLength = 32,
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                    TraceConfiguration = new TraceConfiguration()
                };

                // 验证应用配置对象
                await config.ValidateAsync(ApplicationType.Client);

                // 设置证书验证事件，用于自动接受不受信任的证书
                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
                }

                // 创建一个应用实例对象，用于检查证书
                var application = new ApplicationInstance(config);

                // 检查应用实例对象的证书
                bool check = await application.CheckApplicationInstanceCertificatesAsync(false, 12);

                var ip = protocol.IPAddress;
                var port = int.Parse(protocol.ProtocolPort);

                // 账号密码从Gateway字段获取
                var gatewayParts = protocol.Gateway?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var username = gatewayParts != null && gatewayParts.Length > 0 ? gatewayParts[0] : "";
                var password = gatewayParts != null && gatewayParts.Length > 1 ? gatewayParts[1] : "";

                // 创建一个会话对象，用于连接到 OPC UA 服务器
                EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(config, $"opc.tcp://{ip}:{port}", false);
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(config);
                ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
                _conn = await Session.Create(config, endpoint, false, false, "DataCollector", 60000, new UserIdentity(username, password), null);
            }
        }
        catch (Exception ex)
        {
            if (ex is ProtocolFailedException)
                throw;
            throw new ProtocolException($"{_protocolName}协议连接失败", ex);
        }

        try
        {
            var dataType = Enum.Parse<DataType>(point.DataType);

            var result = new PointCollectTask
            {
                Protocol = protocol,
                Device = device,
                Point = point,
                DataType = dataType
            };

            //测试用
            //var value = _conn.ReadValue(point.Address);
            //Console.WriteLine($"ns=2;s=Tag.应急.Real除尘器应急温度d值是{res1}");

            switch (dataType)
            {
                case DataType.Bool:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.UShort:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.Short:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.UInt:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.Int:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.Float:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.Double:
                    {
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                case DataType.String:
                    {
                        var length = ushort.Parse(point.Length);
                        var res = _conn.ReadValue(point.Address);
                        result.Value = res.Value;
                        break;
                    }
                default:
                    break;
            }

            return result;
        }
        catch (Exception ex)
        {
            if (ex is PointFailedException)
                throw;
            throw new PointException($"{_protocolName}协议读取采集点失败", ex);
        }

    }

    public void Dispose()
    {
        if (_conn != null)
        {
            _conn.Dispose();
            _conn = null;
        }
        GC.SuppressFinalize(this);
    }
}
