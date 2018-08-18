import { PlatformLocation } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";

import { Observable } from "rxjs";

import { ServiceHelpers } from "../service.helpers";

import { IPlexAuthentication, IPlexLibResponse, IPlexOAuthViewModel, IPlexServer, IPlexServerAddViewModel, IPlexServerViewModel, IPlexUserAddResponse, IPlexUserViewModel, IUsersModel } from "../../interfaces";

@Injectable()
export class PlexService extends ServiceHelpers {
    constructor(http: HttpClient, public platformLocation: PlatformLocation) {
        super(http, "/api/v1/Plex/", platformLocation);
    }

    public logIn(login: string, password: string): Observable<IPlexAuthentication> {
        return this.http.post<IPlexAuthentication>(`${this.url}`, JSON.stringify({ login, password }),  {headers: this.headers});
    }

    public getServers(login: string, password: string): Observable<IPlexServerViewModel> {
        return this.http.post<IPlexServerViewModel>(`${this.url}servers`, JSON.stringify({ login, password }),  {headers: this.headers});
    }

    public getServersFromSettings(): Observable<IPlexServerAddViewModel> {
        return this.http.get<IPlexServerAddViewModel>(`${this.url}servers`,  {headers: this.headers});
    }

    public getLibraries(plexSettings: IPlexServer): Observable<IPlexLibResponse> {
        return this.http.post<IPlexLibResponse>(`${this.url}Libraries`, JSON.stringify(plexSettings),  {headers: this.headers});
    }

    public getLibrariesFromSettings(machineId: string): Observable<IPlexLibResponse> {
        return this.http.get<IPlexLibResponse>(`${this.url}Libraries/${machineId}`,  {headers: this.headers});
    }

    public addUserToServer(user: IPlexUserViewModel): Observable<IPlexUserAddResponse> {
        return this.http.post<IPlexUserAddResponse>(`${this.url}user`,JSON.stringify(user),  {headers: this.headers});
    }

    public getFriends(): Observable<IUsersModel[]> {
        return this.http.get<IUsersModel[]>(`${this.url}Friends`,  {headers: this.headers});
    }

    public oAuth(wizard: IPlexOAuthViewModel): Observable<any> {
        return this.http.post<any>(`${this.url}oauth`, JSON.stringify(wizard), {headers: this.headers});
    }
}
