import { JwtHelperService } from '@auth0/angular-jwt';
import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';

@Injectable()
export class AuthGuard implements CanActivate
{
    private jwtHelper = new JwtHelperService(); //injection seems to be broken for angular-jwt
    constructor(private router: Router)
    {

    }
    canActivate()
    {
        var token = localStorage.getItem("jwt");
        if(!token)
        {
            this.router.navigate(["login"]);
            return false;
        }

        if (this.jwtHelper.isTokenExpired(token))
        {
            localStorage.removeItem("jwt");
            this.router.navigate(["login"]);
            return false;
        }
        
        return true;
    }
}