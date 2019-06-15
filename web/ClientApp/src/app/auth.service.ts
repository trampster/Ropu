import { Injectable, Inject } from '@angular/core';
import { HttpClient} from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthService 
{
    public user: User;
    public loggedIn: boolean;
    public loginChangedCallback: () => void;

    constructor(
        private http: HttpClient, 
        @Inject('BASE_URL') private baseUrl: string) { }

    public registerLoginChangedCallback(callback: () => void): void
    {
        this.loginChangedCallback = callback;
    }

    public refresh(): void
    {
        const idToken = localStorage.getItem("jwt");
        this.loggedIn = false;
        if (idToken) 
        {
            this.http.get<User>(this.baseUrl + 'api/Users/Current').subscribe(result => 
            {
                this.loggedIn = true;
                this.user = result;
                this.loginChangedCallback();
            }, error => console.error(error));
        }
        this.loginChangedCallback();
    }

    public getUser(): User
    {
        return this.user;
    }

    public isLoggedIn(): boolean
    {
        return this.loggedIn;
    }

    public logout(): void
    {
        localStorage.removeItem("jwt");
        this.loggedIn = false;
        this.user = null;
        this.loginChangedCallback();
    }
}

export interface User 
{
    name: string;
    id: number;
    imageHash: string;
}