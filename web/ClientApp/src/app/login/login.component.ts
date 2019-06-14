import { HttpClient, HttpHeaders} from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { Router } from "@angular/router";
import { NgForm } from '@angular/forms';
import { AuthService } from '../auth.service';

@Component({
  selector: 'login',
  templateUrl: './login.component.html'
})
export class LoginComponent {
  invalidLogin: boolean;

  constructor(
    private router: Router, 
    private http: HttpClient, 
    @Inject('BASE_URL') private baseUrl: string,
    private authService: AuthService) 
  { 

  }

  login(form: NgForm) {
    let credentials = JSON.stringify(form.value);
    this.http.post(this.baseUrl + 'api/Login', credentials, {
      headers: new HttpHeaders({
        "Content-Type": "application/json"
      })
    }).subscribe(response => {
      let token = (<any>response).token;
      localStorage.setItem("jwt", token);
      this.invalidLogin = false;
      this.router.navigate(["/"]);
      this.authService.refresh();
    }, err => {
      this.invalidLogin = true;
      this.authService.refresh();
    });
  }

}
