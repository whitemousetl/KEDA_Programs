﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceStatusNotifier;
public class NotificationModel
{
    public string edge_id {  get; set; }
    public string edge_name {  get; set; }
    public string ip {  get; set; }
    public string status { get; set; }
    public string msg {  get; set; }
    public List<DeviceStatus> items { get; set; }
    public string desc { get; set; }
    public string time { get; set; }
}

public class DeviceStatus
{
    public string equipment_id {  get; set; }
    public string equipment_name { get;set; }
    public string equipment_ip { get; set; }
    public string equipment_status { get; set; }
    public string msg { get; set; }
    public string desc {  get; set; }
}
