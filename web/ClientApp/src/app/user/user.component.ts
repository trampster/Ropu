import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';
import { AuthService } from '../auth.service';
import { FormGroup, FormControl } from '@angular/forms';

@Component({
  selector: 'app-user-component',
  templateUrl: './user.component.html'
})
export class UserComponent {
  public id: string;
  public user: User;
  loaded: boolean;
  editable: boolean;
  nameFormData: FormGroup;

  constructor(private a: ActivatedRoute, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private authService: AuthService) {
    this.loaded = false;
    this.editable = false;
  }

  ngOnInit() {
    this.a.params.subscribe(params => {
      this.id = this.a.snapshot.params.userid;
      this.http.get<User>(this.baseUrl + 'api/Users/' + this.id).subscribe(result => {
        this.user = result;
        this.loaded = true;
        this.nameFormData = new FormGroup({
          name: new FormControl(this.user.name),
       });
      }, error => console.error(error));

      this.http.get<boolean>(this.baseUrl + 'api/Users/' + this.id + '/CanEdit').subscribe(result => {
        this.editable = result;
      }, error => console.error(error));
    });
  }

  editUser(user): void {
    this.user.name = user.name;
    console.debug("edit User" + user.name);

    this.http.post<User>(this.baseUrl + 'api/Users/Edit', JSON.stringify(this.user),{
      headers: new HttpHeaders({
        "Content-Type": "application/json"
      })
    }).subscribe(result => {}, 
      error => console.error(error));
  }

}

interface User {
  name: string;
  id: number;
  imageHash: string;
}
