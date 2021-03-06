﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ICommunicate
{
    List<Request> RequestList { get; set; }

    void Send(uint? nextHop, Request request);
    void Receive(Request request);
    Task<Response> SendAsync(uint? nextHop, Request request, int timeout, int attempts);
    void Send(uint? nextHop, Response response);
    void Receive(Response response);
    List<uint?> Discover();
    Request FetchNextRequest();
}
