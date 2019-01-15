﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GWLogger.Inventory.Controller {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="https://inventory.psi.ch/manage/DirectCommands.asmx", ConfigurationName="Inventory.Controller.DirectCommandsSoap")]
    public interface DirectCommandsSoap {
        
        [System.ServiceModel.OperationContractAttribute(Action="https://inventory.psi.ch/manage/DirectCommands.asmx/SendEpicsCommand", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string SendEpicsCommand(string config, string channel, string slave);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://inventory.psi.ch/manage/DirectCommands.asmx/SendEpicsCommand", ReplyAction="*")]
        System.Threading.Tasks.Task<string> SendEpicsCommandAsync(string config, string channel, string slave);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://inventory.psi.ch/manage/DirectCommands.asmx/StartTask", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string StartTask(string slave, string user, string task);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://inventory.psi.ch/manage/DirectCommands.asmx/StartTask", ReplyAction="*")]
        System.Threading.Tasks.Task<string> StartTaskAsync(string slave, string user, string task);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface DirectCommandsSoapChannel : GWLogger.Inventory.Controller.DirectCommandsSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class DirectCommandsSoapClient : System.ServiceModel.ClientBase<GWLogger.Inventory.Controller.DirectCommandsSoap>, GWLogger.Inventory.Controller.DirectCommandsSoap {
        
        public DirectCommandsSoapClient() {
        }
        
        public DirectCommandsSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public DirectCommandsSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DirectCommandsSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DirectCommandsSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string SendEpicsCommand(string config, string channel, string slave) {
            return base.Channel.SendEpicsCommand(config, channel, slave);
        }
        
        public System.Threading.Tasks.Task<string> SendEpicsCommandAsync(string config, string channel, string slave) {
            return base.Channel.SendEpicsCommandAsync(config, channel, slave);
        }
        
        public string StartTask(string slave, string user, string task) {
            return base.Channel.StartTask(slave, user, task);
        }
        
        public System.Threading.Tasks.Task<string> StartTaskAsync(string slave, string user, string task) {
            return base.Channel.StartTaskAsync(slave, user, task);
        }
    }
}
