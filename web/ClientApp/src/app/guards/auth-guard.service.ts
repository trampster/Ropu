import { JwtHelperService } from '@auth0/angular-jwt';
import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';

@Injectable()
export class AuthGuard implements CanActivate {
  private jwtHelper = new JwtHelperService(); //injection seems to be broken for angular-jwt
  constructor(private router: Router) {
    
  }
  canActivate() {
    var token = localStorage.getItem("jwt");

    if (token && !this.jwtHelper.isTokenExpired(token)){
      console.log(this.jwtHelper.decodeToken(token));
      return true;
    }
    localStorage.remoteItem("jwt");
    this.router.navigate(["login"]);
    return false;
  }
}