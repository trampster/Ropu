import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-users-component',
  templateUrl: './users.component.html'
})
export class UsersComponent {
  public users: User[];

  constructor(http: HttpClient, private router: Router, @Inject('BASE_URL') private baseUrl: string) {
    http.get<User[]>(baseUrl + 'api/Users/Users').subscribe(result => {
      this.users = result;
    }, error => console.error(error));
  }

  showUser(user: User) 
  {
    this.router.navigate(['/user/' + user.id]);
  }
}

interface User {
  name: string;
  id: number;
  imageHash: string;
}
