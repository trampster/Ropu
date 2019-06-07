import { HttpClient, HttpHeaders} from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { Router } from "@angular/router";
import { NgForm } from '@angular/forms';

@Component({
  selector: 'create-user-component',
  templateUrl: './create-user.component.html'
})
export class CreateUserComponent {
  invalidUser: boolean;

  constructor(private router: Router, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) 
  { 

  }

  create(form: NgForm) {
    let userDetails = JSON.stringify(form.value);
    this.http.post(this.baseUrl + 'api/users/create', userDetails, {
      headers: new HttpHeaders({
        "Content-Type": "application/json"
      })
    }).subscribe(response => {
      this.invalidUser = false;
    }, err => {
      this.invalidUser = true;
    });
  }

}
